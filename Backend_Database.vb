Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Text.Json
Imports MySqlConnector

Namespace SidhawanForProfsAndAdmins

    Public NotInheritable Class Db

        ' Prevent instantiation: this class is only a container for Shared members.
        Private Sub New()
        End Sub

        ' ---------- private internals ----------

        Private Shared ReadOnly _connectionString As Lazy(Of String) =
            New Lazy(Of String)(AddressOf LoadConnectionString)

        Public Shared ReadOnly Property ConnectionString As String
            Get
                Return _connectionString.Value
            End Get
        End Property

        Private Shared Function LoadConnectionString() As String
            Dim configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json")
            If Not File.Exists(configPath) Then
                Throw New FileNotFoundException("Database configuration file was not found.", configPath)
            End If

            Using document = JsonDocument.Parse(File.ReadAllText(configPath))
                Dim connectionStrings = document.RootElement.GetProperty("ConnectionStrings")
                Dim connectionStringElement As JsonElement

                If Not connectionStrings.TryGetProperty("SidhawanDB", connectionStringElement) AndAlso
                   Not connectionStrings.TryGetProperty("Default", connectionStringElement) Then
                    Throw New InvalidOperationException("ConnectionStrings:SidhawanDB is missing from appsettings.json.")
                End If

                Dim value As String

                If connectionStringElement.ValueKind = JsonValueKind.Object Then
                    Dim parts As New List(Of String)()

                    For Each propertyItem In connectionStringElement.EnumerateObject()
                        parts.Add(propertyItem.Name & "=" & propertyItem.Value.GetString() & ";")
                    Next

                    value = String.Join("", parts)
                Else
                    value = connectionStringElement.GetString()
                End If

                If String.IsNullOrWhiteSpace(value) Then
                    Throw New InvalidOperationException("ConnectionStrings:SidhawanDB cannot be empty.")
                End If

                Return value
            End Using
        End Function

        Private Shared Function CreateCommand(conn As MySqlConnection, sql As String, args As (String, Object)()) As MySqlCommand
            Dim cmd = New MySqlCommand(sql, conn)
            If args IsNot Nothing Then
                For Each field In args
                    cmd.Parameters.AddWithValue(field.Item1, If(field.Item2, DBNull.Value))
                Next
            End If
            Return cmd
        End Function

        ' ---------- public API ----------

        ' Runs a SELECT and maps each row to a T using the supplied function.
        Public Shared Function Query(Of T)(sql As String,
                                           map As Func(Of MySqlDataReader, T),
                                           ParamArray args As (String, Object)()) As List(Of T)
            Dim rows = New List(Of T)()
            Using conn = New MySqlConnection(ConnectionString)
                conn.Open()
                Using cmd = CreateCommand(conn, sql, args)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            rows.Add(map(reader))
                        End While
                    End Using
                End Using
            End Using
            Return rows
        End Function

        ' Runs INSERT/UPDATE/DELETE and returns the number of affected rows.
        Public Shared Function Execute(sql As String, ParamArray args As (String, Object)()) As Integer
            Using conn = New MySqlConnection(ConnectionString)
                conn.Open()
                Using cmd = CreateCommand(conn, sql, args)
                    Return cmd.ExecuteNonQuery()
                End Using
            End Using
        End Function

        ' Network testing: returns Nothing on success, or the error text on failure.
        Public Shared Function TestConnection() As String
            Try
                Using conn = New MySqlConnection(ConnectionString)
                    conn.Open()
                End Using
                Return Nothing
            Catch ex As Exception
                Return ex.Message
            End Try
        End Function

    End Class

    ' Reading of Num and Str data
    Public Module DbReaderExtensions

        <Extension()>
        Public Function Str(r As MySqlDataReader, column As String) As String
            Dim i = r.GetOrdinal(column)
            If r.IsDBNull(i) Then Return ""
            Return If(Convert.ToString(r.GetValue(i)), "")
        End Function

        <Extension()>
        Public Function Num(r As MySqlDataReader, column As String) As String
            Dim i = r.GetOrdinal(column)
            If r.IsDBNull(i) Then Return ""
            Dim text As String = If(Convert.ToString(r.GetValue(i)), "")
            Dim d As Decimal
            If Decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, d) Then
                Return d.ToString("0.00", CultureInfo.InvariantCulture)
            End If
            Return text
        End Function

    End Module


    Public Module Database
        Private ReadOnly ConnectionString As String = Db.ConnectionString
        Private _attendanceSchemaReady As Boolean
        Public Function LoadAdminProfile(employeeNumber As String) As Boolean
            Const sql As String =
                "SELECT " &
                "`UserID` AS `EmployeeNumber`, `FName`, `MName`, `LName`, `Suffix`, " &
                "`DOB`, `Sex`, `ConNum` AS `ContactNumber`, `Email` AS `EmailAddress`, " &
                "`DepartmentID` AS `Department`, `Position`, `IsAdmin` " &
                "FROM `employee` " &
                "WHERE `UserID` = @EmployeeNumber " &
                "AND `IsAdmin` = 1 " &
                "LIMIT 1;"

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    command.Parameters.AddWithValue(
                        "@EmployeeNumber",
                        employeeNumber)

                    connection.Open()

                    Using reader = command.ExecuteReader()
                        If Not reader.Read() Then
                            Return False
                        End If

                        Data.Admin = New Person With {
                            .Id = Value(reader, "EmployeeNumber"),
                            .FirstName = Value(reader, "FName"),
                            .MiddleName = Value(reader, "MName"),
                            .LastName = Value(reader, "LName"),
                            .Suffix = Value(reader, "Suffix"),
                            .BirthDate = FormatDate(reader, "DOB"),
                            .Sex = Value(reader, "Sex"),
                            .ContactNumber = Value(reader, "ContactNumber"),
                            .Email = Value(reader, "EmailAddress")
                        }

                        Data.Admin.Academic.Clear()
                        Data.Admin.Academic.Add(
                            New Field("Department", Value(reader, "Department")))
                        Data.Admin.Academic.Add(
                            New Field("Position", Value(reader, "Position")))

                        Return True
                    End Using
                End Using
            End Using
        End Function

        Public Function LoadDepartments() As List(Of String)
            Const sql As String =
                "SELECT `DepartmentName` " &
                "FROM `department` " &
                "WHERE `DepartmentID` <> 'RO' " &
                "AND `DepartmentName` IS NOT NULL " &
                "AND TRIM(`DepartmentName`) <> '' " &
                "ORDER BY `DepartmentName`;"

            Return Db.Query(
                sql,
                Function(reader) reader.Str("DepartmentName"))
        End Function

        Public Function GetEmployeeNumbers() As List(Of String)
            Const sql As String =
                "SELECT `EmployeeNumber` " &
                "FROM `tbl_employees`;"

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    connection.Open()

                    Using reader = command.ExecuteReader()
                        Dim employeeNumbers As New List(Of String)

                        While reader.Read()
                            employeeNumbers.Add(Value(reader, "EmployeeNumber"))
                        End While

                        Return employeeNumbers
                    End Using
                End Using
            End Using
        End Function

        Public Function LoadAdminStudents() As List(Of StudentRow)
            Const sql As String =
                "SELECT " &
                "`StudentNo`, `StudentName`, `Status`, `YearLevel`, " &
                "`ProgramID`, `ProgramName`, `DepartmentID`, `DepartmentName`, " &
                "`SchoolYear`, `Semester`, `Section` " &
                "FROM `vw_adminstudents`;"

            Dim students As New List(Of StudentRow)()

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    connection.Open()

                    Using reader = command.ExecuteReader()
                        While reader.Read()
                            students.Add(
                                New StudentRow With {
                                    .StudentNo = Value(reader, "StudentNo"),
                                    .Name = Value(reader, "StudentName"),
                                    .Status = Value(reader, "Status"),
                                    .YearLevel = Value(reader, "YearLevel"),
                                    .ProgramID = Value(reader, "ProgramID"),
                                    .ProgramName = Value(reader, "ProgramName"),
                                    .DepartmentID = Value(reader, "DepartmentID"),
                                    .DepartmentName = Value(reader, "DepartmentName"),
                                    .SchoolYear = Value(reader, "SchoolYear"),
                                    .Semester = Value(reader, "Semester"),
                                    .Section = Value(reader, "Section")
                                })
                        End While
                    End Using
                End Using
            End Using

            Return students
        End Function

        Public Function LoadAdminDetailedSummary() As List(Of CourseRow)
            Const sql As String =
    "SELECT " &
    "MIN(s.ClassID) AS ClassID, " &
    "s.DepartmentID, " &
    "s.DepartmentName, " &
    "s.ProgramID, " &
    "s.ProgramName, " &
    "s.SchoolYear AS SchoolYear, " &
    "MIN(s.Semester) AS Semester, " &
    "s.Section, " &
    "s.CourseCode, " &
    "s.CourseName, " &
    "s.ProfessorId, " &
    "s.Professor, " &
    "s.UnitsEarned, " &
    "MIN(st.YearLevel) AS YearLevel " &
    "FROM vw_adminsummaryofrecord AS s " &
    "INNER JOIN vw_adminstudents AS st " &
    "ON TRIM(st.ProgramID) = TRIM(s.ProgramID) " &
    "AND TRIM(st.SchoolYear) = TRIM(s.SchoolYear) " &
    "AND TRIM(st.Section) = TRIM(s.Section) " &
    "GROUP BY " &
    "s.DepartmentID, s.DepartmentName, " &
    "s.ProgramID, s.ProgramName, s.SchoolYear, s.Section, " &
    "s.CourseCode, s.CourseName, " &
    "s.ProfessorId, s.Professor, s.UnitsEarned;"

            Dim summaries As New List(Of CourseRow)()

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    connection.Open()

                    Using reader = command.ExecuteReader()
                        While reader.Read()
                            summaries.Add(
                                New CourseRow With {
                                    .ClassID = Value(reader, "ClassID"),
                                    .DepartmentID = Value(reader, "DepartmentID"),
                                    .DepartmentName = Value(reader, "DepartmentName"),
                                    .ProgramID = Value(reader, "ProgramID"),
                                    .ProgramName = Value(reader, "ProgramName"),
                                    .SchoolYear = Value(reader, "SchoolYear"),
                                    .Semester = Value(reader, "Semester"),
                                    .Section = Value(reader, "Section"),
                                    .CourseCode = Value(reader, "CourseCode"),
                                    .CourseName = Value(reader, "CourseName"),
                                    .ProfessorId = Value(reader, "ProfessorId"),
                                    .Professor = Value(reader, "Professor"),
                                    .Units = Value(reader, "UnitsEarned")
                                })
                        End While
                    End Using
                End Using
            End Using

            Return summaries
        End Function

        Public Function LoadAdminClassSummary(classId As String) As List(Of ProfessorSummaryRow)
            Const sql As String =
                "SELECT `StudentName`, `Attendance`, `WrittenWork`, " &
                "`PerformanceTask`, `MajorExam`, `MidtermGrade`, " &
                "`FinalGrade`, `OverallGrade`, `Remarks`, `GWA` " &
                "FROM `vw_admindetailedsummaryofrecord` " &
                "WHERE `ClassID` = @ClassID " &
                "ORDER BY `StudentName`;"

            Return Db.Query(
                sql,
                Function(reader)
                    Return New ProfessorSummaryRow With {
                        .StudentName = reader.Str("StudentName"),
                        .Attendance = reader.Str("Attendance"),
                        .WrittenWork = reader.Str("WrittenWork"),
                        .PerformanceTask = reader.Str("PerformanceTask"),
                        .MajorExam = reader.Str("MajorExam"),
                        .MidtermGrade = reader.Str("MidtermGrade"),
                        .FinalGrade = reader.Str("FinalGrade"),
                        .OverallGrade = reader.Str("OverallGrade"),
                        .Remarks = reader.Str("Remarks"),
                        .Gwa = reader.Str("GWA")
                    }
                End Function,
                ("@ClassID", classId))
        End Function

        Public Function LoadAdminInstructors() As List(Of StaffRow)
            Const sql As String =
                "SELECT " &
                "`EmployeeID`, `ProfessorName`, `EmployementType`, " &
                "`DepartmentID`, `DepartmentName`, `IsAdmin` " &
                "FROM `vw_adminprofessors`;"

            Dim professors As New List(Of StaffRow)()

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    connection.Open()

                    Using reader = command.ExecuteReader()
                        While reader.Read()
                            professors.Add(
                                New StaffRow With {
                                    .Id = Value(reader, "EmployeeID"),
                                    .Name = Value(reader, "ProfessorName"),
                                    .EmploymentType = Value(reader, "EmployementType"),
                                    .DepartmentID = Value(reader, "DepartmentID"),
                                    .DepartmentName = Value(reader, "DepartmentName"),
                                    .IsAdmin = Value(reader, "IsAdmin") = "1"
                                })
                        End While
                    End Using
                End Using
            End Using

            Return professors
        End Function

        Public Function LoadAdminGradePermissions() As List(Of StaffRow)
            Const sql As String =
        "SELECT " &
        "`EmployeeID`, `InstructorName`, `EmploymentType`, " &
        "`DepartmentID`, `DepartmentName`, `PermissionStatus` " &
        "FROM `vw_admingradepermissions`;"

            Dim instructors As New List(Of StaffRow)()

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    connection.Open()

                    Using reader = command.ExecuteReader()
                        While reader.Read()
                            Dim permissionStatus =
                        Value(reader, "PermissionStatus").Trim()

                            instructors.Add(
                        New StaffRow With {
                            .Id = Value(reader, "EmployeeID"),
                            .Name = Value(reader, "InstructorName"),
                            .EmploymentType = Value(reader, "EmploymentType"),
                            .DepartmentID = Value(reader, "DepartmentID"),
                            .DepartmentName = Value(reader, "DepartmentName"),
                            .IsLocked = String.Equals(
                                permissionStatus,
                                "Locked",
                                StringComparison.OrdinalIgnoreCase)
                        })
                        End While
                    End Using
                End Using
            End Using

            Return instructors
        End Function

        Public Function IsProfessorGradeEditingLocked(professorId As String) As Boolean
            Const sql As String =
                "SELECT COUNT(*) AS LockedCount " &
                "FROM `gradeediting` " &
                "WHERE `ProfessorID` = @ProfessorID " &
                "AND `Status` = 'Locked';"

            Dim rows = Db.Query(
                sql,
                Function(reader) Convert.ToInt32(reader("LockedCount")) > 0,
                ("@ProfessorID", professorId))

            Return rows.Count > 0 AndAlso rows(0)
        End Function

        Public Sub SetGradePermission(
            employeeId As String,
            departmentId As String,
            isLocked As Boolean,
            adminEmployeeNumber As String)

            Const existsSql As String =
                "SELECT COUNT(*) " &
                "FROM `gradeediting` " &
                "WHERE `ProfessorID` = @ProfessorID " &
                "AND `DepartmentID` = @DepartmentID;"

            Const updateSql As String =
                "UPDATE `gradeediting` " &
                "SET `Status` = @Status " &
                "WHERE `ProfessorID` = @ProfessorID " &
                "AND `DepartmentID` = @DepartmentID;"

            Const insertSql As String =
                "INSERT INTO `gradeediting` " &
                "(`DepartmentID`, `ProfessorID`, `Status`) " &
                "VALUES (@DepartmentID, @ProfessorID, @Status);"

            Dim status = If(isLocked, "Locked", "Unlocked")

            Using connection As New MySqlConnection(ConnectionString)
                connection.Open()

                Using transaction = connection.BeginTransaction()
                    Try
                        Dim exists As Boolean

                        Using existsCommand As New MySqlCommand(
                            existsSql,
                            connection,
                            transaction)

                            existsCommand.Parameters.AddWithValue(
                                "@ProfessorID",
                                employeeId)

                            existsCommand.Parameters.AddWithValue(
                                "@DepartmentID",
                                departmentId)

                            exists =
                                Convert.ToInt32(
                                    existsCommand.ExecuteScalar()) > 0
                        End Using

                        If exists Then
                            Using updateCommand As New MySqlCommand(
                                updateSql,
                                connection,
                                transaction)

                                updateCommand.Parameters.AddWithValue(
                                    "@ProfessorID",
                                    employeeId)

                                updateCommand.Parameters.AddWithValue(
                                    "@DepartmentID",
                                    departmentId)

                                updateCommand.Parameters.AddWithValue(
                                    "@Status",
                                    status)

                                updateCommand.ExecuteNonQuery()
                            End Using
                        Else
                            Using insertCommand As New MySqlCommand(
                                insertSql,
                                connection,
                                transaction)

                                insertCommand.Parameters.AddWithValue(
                                    "@DepartmentID",
                                    departmentId)

                                insertCommand.Parameters.AddWithValue(
                                    "@ProfessorID",
                                    employeeId)

                                insertCommand.Parameters.AddWithValue(
                                    "@Status",
                                    status)

                                insertCommand.ExecuteNonQuery()
                            End Using
                        End If

                        transaction.Commit()
                    Catch
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using

            Dim actionText = If(isLocked, "locked", "unlocked")

            InsertHistory(
                adminEmployeeNumber,
                adminEmployeeNumber &
                " has " &
                actionText &
                " grade editing for " &
                employeeId &
                ".")
        End Sub

        Public Sub SetAllGradePermissions(
    isLocked As Boolean,
    adminEmployeeNumber As String)
            Const updateSql As String =
                "UPDATE `gradeediting` As ge " &
                "INNER JOIN `employee` As e " &
                "On e.`UserID` = ge.`ProfessorID` " &
                "And e.`DepartmentID` = ge.`DepartmentID` " &
                "Set ge.`Status` = @Status " &
                "WHERE e.`Role` = 'Professor';"

            Const insertSql As String =
                "INSERT INTO `gradeediting` " &
                "(`ProfessorID`, `DepartmentID`, `Status`) " &
                "Select e.`UserID`, e.`DepartmentID`, @Status " &
                "FROM `employee` As e " &
                "LEFT JOIN `gradeediting` As ge " &
                "On ge.`ProfessorID` = e.`UserID` " &
                "And ge.`DepartmentID` = e.`DepartmentID` " &
                "WHERE e.`Role` = 'Professor' " &
                "And ge.`ProfessorID` Is NULL;"

            Dim status = If(isLocked, "Locked", "Unlocked")

            Using connection As New MySqlConnection(ConnectionString)
                connection.Open()

                Using transaction = connection.BeginTransaction()
                    Try
                        Using command As New MySqlCommand(
                            updateSql,
                            connection,
                            transaction)

                            command.Parameters.AddWithValue("@Status", status)
                            command.ExecuteNonQuery()
                        End Using

                        Using command As New MySqlCommand(
                            insertSql,
                            connection,
                            transaction)

                            command.Parameters.AddWithValue("@Status", status)
                            command.ExecuteNonQuery()
                        End Using

                        transaction.Commit()
                        Dim actionText = If(isLocked, "locked", "unlocked")

                        InsertHistory(
                            adminEmployeeNumber,
                            adminEmployeeNumber &
                            " has " &
                            actionText &
                            " the grade editing for ALL departments.")
                    Catch
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        Public Sub InsertHistory(
            userId As String,
            changeDescription As String)

            Const sql As String =
                "INSERT INTO `history_admin` " &
                "(`UserID`, `changedat`, `Description`) " &
                "VALUES (@UserID, NOW(), @Description);"

            Dim description = If(
                changeDescription.Length > 100,
                changeDescription.Substring(0, 100),
                changeDescription)

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    command.Parameters.AddWithValue("@UserID", userId)
                    command.Parameters.AddWithValue("@Description", description)

                    connection.Open()
                    command.ExecuteNonQuery()
                End Using
            End Using

        End Sub

        Public Function LoadHistory() As List(Of HistoryRow)
            Const sql As String =
                "Select `IDNo`, `LogDateTime`, `ChangesMade` " &
                "FROM `vw_adminhistory` " &
                "ORDER BY `LogDateTime` DESC;"

            Dim rows As New List(Of HistoryRow)()

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    connection.Open()

                    Using reader = command.ExecuteReader()
                        While reader.Read()
                            rows.Add(
                                New HistoryRow With {
                                    .Id = Value(reader, "IDNo"),
                                    .When = FormatStamp(reader, "LogDateTime"),
                                    .Change = Value(reader, "ChangesMade")
                                })
                        End While
                    End Using
                End Using
            End Using

            Return rows
        End Function

        Public Sub ReloadHistory()
            Dim rows = LoadHistory()

            Data.History.Clear()

            For Each row In rows
                Data.History.Add(row)
            Next
        End Sub

        Public Sub LogGradeUpdate(
            employeeId As String,
            studentNo As String,
            term As String,
            courseCode As String)

            InsertHistory(
                employeeId,
                "UPDATED " &
                studentNo &
                "'s " &
                term.ToUpperInvariant() &
                " " &
                courseCode &
                " grade.")
        End Sub

        Public Sub LogOwnProfileUpdate(employeeId As String)
            InsertHistory(
                employeeId,
                employeeId & " UPDATED their profile.")
        End Sub

        Public Sub LogStudentProfileUpdate(
            employeeId As String,
            studentNo As String)

            InsertHistory(
                employeeId,
                employeeId &
                " UPDATED " &
                studentNo &
                "'s profile.")
        End Sub

        Public Sub LogEmployeeProfileUpdate(
            actorEmployeeId As String,
            targetEmployeeId As String)

            InsertHistory(
                actorEmployeeId,
                actorEmployeeId &
                " UPDATED " &
                targetEmployeeId &
                "'s profile.")
        End Sub

        Private Function ParseBirthDate(text As String) As Date
            Dim d As Date
            If Date.TryParseExact(text,
                    New String() {"MM/dd/yyyy", "M/d/yyyy", "yyyy-MM-dd"},
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, d) Then
                Return d
            End If
            Throw New FormatException("Birth date must look like 02/24/1995.")
        End Function

        Public Function UpdateEmployeeProfile(
            p As Person,
            originalId As String) As Boolean

            Const sql As String =
                "UPDATE `employee` SET " &
                "`Fname` = @f, " &
                "`Mname` = @m, " &
                "`Lname` = @l, " &
                "`Suffix` = @s, " &
                "`DOB` = @dob, " &
                "`Sex` = @sex, " &
                "`ConNum` = @con, " &
                "`Email` = @email " &
                "WHERE `UserID` = @id;"

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    command.Parameters.AddWithValue("@f", p.FirstName)
                    command.Parameters.AddWithValue("@m", p.MiddleName)
                    command.Parameters.AddWithValue("@l", p.LastName)
                    command.Parameters.AddWithValue("@s", If(p.Suffix = "N/A", "", p.Suffix))
                    command.Parameters.AddWithValue("@dob", ParseBirthDate(p.BirthDate))
                    command.Parameters.AddWithValue("@sex", p.Sex)
                    command.Parameters.AddWithValue("@con", p.ContactNumber)
                    command.Parameters.AddWithValue("@email", p.Email)
                    command.Parameters.AddWithValue("@id", originalId)

                    connection.Open()
                    Return command.ExecuteNonQuery() > 0
                End Using
            End Using
        End Function

        Public Function UpdateStudentProfile(
            p As Person,
            originalId As String,
            actorUserId As String) As Boolean

            Const updateSql As String =
                "UPDATE `student` SET " &
                "`student_Fname` = @f, " &
                "`student_Mname` = @m, " &
                "`student_Lname` = @l, " &
                "`student_Suffix` = @s, " &
                "`student_DOB` = @dob, " &
                "`student_Sex` = @sex, " &
                "`student_ConNum` = @con, " &
                "`student_Email` = @email " &
                "WHERE `StudentID` = @id;"

            Const historySql As String =
                "INSERT INTO `history_admin` " &
                "(`UserID`, `changedat`, `Description`) " &
                "VALUES (@UserID, NOW(), @Description);"

            Dim description =
                actorUserId & " UPDATED " & originalId & "'s profile."

            If description.Length > 100 Then
                description = description.Substring(0, 100)
            End If

            Using connection As New MySqlConnection(ConnectionString)
                connection.Open()

                Using transaction = connection.BeginTransaction()
                    Try
                        Using command As New MySqlCommand(
                            updateSql,
                            connection,
                            transaction)

                            command.Parameters.AddWithValue("@f", p.FirstName)
                            command.Parameters.AddWithValue("@m", p.MiddleName)
                            command.Parameters.AddWithValue("@l", p.LastName)
                            command.Parameters.AddWithValue(
                                "@s",
                                If(p.Suffix = "N/A", "", p.Suffix))
                            command.Parameters.AddWithValue(
                                "@dob",
                                ParseBirthDate(p.BirthDate))
                            command.Parameters.AddWithValue("@sex", p.Sex)
                            command.Parameters.AddWithValue("@con", p.ContactNumber)
                            command.Parameters.AddWithValue("@email", p.Email)
                            command.Parameters.AddWithValue("@id", originalId)

                            If command.ExecuteNonQuery() = 0 Then
                                transaction.Rollback()
                                Return False
                            End If
                        End Using

                        Using command As New MySqlCommand(
                            historySql,
                            connection,
                            transaction)

                            command.Parameters.AddWithValue("@UserID", actorUserId)
                            command.Parameters.AddWithValue(
                                "@Description",
                                description)

                            command.ExecuteNonQuery()
                        End Using

                        transaction.Commit()
                        Return True
                    Catch
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Private Function FormatStamp(reader As MySqlDataReader, columnName As String) As String
            Dim ordinal = reader.GetOrdinal(columnName)
            If reader.IsDBNull(ordinal) Then Return ""
            Return reader.GetDateTime(ordinal).ToString(
                "yyyy-MM-dd HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture)
        End Function

        Private Function Value(
            reader As MySqlDataReader,
            columnName As String) As String

            If reader.IsDBNull(reader.GetOrdinal(columnName)) Then
                Return ""
            End If

            Return reader(columnName).ToString()
        End Function

        Private Function FormatDate(
            reader As MySqlDataReader,
            columnName As String) As String

            If reader.IsDBNull(reader.GetOrdinal(columnName)) Then
                Return ""
            End If

            Dim dateValue As DateTime

            If DateTime.TryParse(
                reader(columnName).ToString(),
                dateValue) Then

                Return dateValue.ToString("MM/dd/yyyy")
            End If

            Return ""
        End Function

        Private Function AcademicValue(
            person As Person,
            label As String) As String

            For Each item In person.Academic
                If String.Equals(
                    item.Label,
                    label,
                    StringComparison.OrdinalIgnoreCase) Then

                    Return item.Value
                End If
            Next

            Return ""
        End Function

        Private Function GetInstructorUserId(
            employeeId As String) As String

            Const sql As String =
                "SELECT `UserID` " &
                "FROM `vw_instructorprofile` " &
                "WHERE `EmployeeID` = @EmployeeID " &
                "LIMIT 1;"

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    command.Parameters.AddWithValue(
                        "@EmployeeID",
                        employeeId)

                    connection.Open()

                    Dim result = command.ExecuteScalar()

                    If result Is Nothing OrElse result Is DBNull.Value Then
                        Throw New InvalidOperationException(
                            "No UserID was found for employee ID: " &
                            employeeId)
                    End If

                    Return result.ToString()
                End Using
            End Using
        End Function

        Public Function LoadInstructorProfile(employeeNumber As String) As Person
            Const sql As String =
                "SELECT " &
                "`EmployeeNumber`, `Fname`, `Mname`, `Lname`, `Suffix`, " &
                "`DOB`, `Sex`, `ContactNumber`, `EmailAddress`, " &
                "`College`, `Position` " &
                "FROM `vw_professorprofile` " &
                "WHERE `EmployeeNumber` = @EmployeeNumber " &
                "LIMIT 1;"

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    command.Parameters.AddWithValue(
                        "@EmployeeNumber",
                        employeeNumber)

                    connection.Open()

                    Using reader = command.ExecuteReader()
                        If Not reader.Read() Then
                            Return Nothing
                        End If

                        Dim instructor As New Person With {
                            .Id = Value(reader, "EmployeeNumber"),
                            .FirstName = Value(reader, "Fname"),
                            .MiddleName = Value(reader, "Mname"),
                            .LastName = Value(reader, "Lname"),
                            .Suffix = Value(reader, "Suffix"),
                            .BirthDate = FormatDate(reader, "DOB"),
                            .Sex = Value(reader, "Sex"),
                            .ContactNumber = Value(reader, "ContactNumber"),
                            .Email = Value(reader, "EmailAddress")
                        }

                        instructor.Academic.Clear()
                        instructor.Academic.Add(
                            New Field("College", Value(reader, "College")))
                        instructor.Academic.Add(
                            New Field("EmploymentType", Value(reader, "Position")))

                        Return instructor
                    End Using
                End Using
            End Using
        End Function

        Public Function LoadStudentProfile(studentId As String) As Person
            Const sql As String =
    "SELECT " &
    "`p`.`StudentID`, `p`.`student_Fname`, `p`.`student_Mname`, " &
    "`p`.`student_Lname`, `p`.`student_Suffix`, `p`.`student_DOB`, " &
    "`p`.`student_Sex`, `p`.`student_ConNum`, `p`.`student_Email`, " &
    "`a`.`YearLevel`, `a`.`ProgramName`, " &
    "`a`.`DepartmentName` AS `CollegeName`, " &
    "`a`.`SchoolYear`, `a`.`Section` " &
    "FROM `vw_studentprofile` AS p " &
    "LEFT JOIN `vw_adminstudents` AS a " &
    "ON `a`.`StudentNo` = `p`.`StudentID` " &
    "WHERE `p`.`StudentID` = @StudentID " &
    "LIMIT 1;"

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    command.Parameters.AddWithValue("@StudentID", studentId)
                    connection.Open()

                    Using reader = command.ExecuteReader()
                        If Not reader.Read() Then Return Nothing

                        Dim student As New Person With {
                            .Id = Value(reader, "StudentID"),
                            .FirstName = Value(reader, "student_Fname"),
                            .MiddleName = Value(reader, "student_Mname"),
                            .LastName = Value(reader, "student_Lname"),
                            .Suffix = Value(reader, "student_Suffix"),
                            .BirthDate = FormatDate(reader, "student_DOB"),
                            .Sex = Value(reader, "student_Sex"),
                            .ContactNumber = Value(reader, "student_ConNum"),
                            .Email = Value(reader, "student_Email")
                        }

                        student.Academic.Clear()
                        student.Academic.Add(
                            New Field("Year Level", Value(reader, "YearLevel")))
                        student.Academic.Add(
                            New Field("Program", Value(reader, "ProgramName")))
                        student.Academic.Add(
                            New Field("College", Value(reader, "CollegeName")))
                        student.Academic.Add(
                            New Field("School Year", Value(reader, "SchoolYear")))
                        student.Academic.Add(
                            New Field("Section", Value(reader, "Section")))

                        Return student
                    End Using
                End Using
            End Using
        End Function
        Public Function WholePercentageFromDatabase(
            databaseValue As String) As String

            Dim value As Decimal
            If Not Decimal.TryParse(
                If(databaseValue, "").Trim(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                value) Then

                Throw New FormatException("The database contains an invalid grading percentage.")
            End If

            Return (value * 100D).ToString(
                "0.##",
                CultureInfo.InvariantCulture)
        End Function

        Public Function DatabasePercentageFromWhole(
            wholeValue As String) As Decimal

            Dim text = If(wholeValue, "").Trim().TrimEnd("%"c)
            Dim value As Decimal

            If Not Decimal.TryParse(
                text,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                value) OrElse
               value < 0D OrElse
               value > 100D OrElse
               value <> Decimal.Truncate(value) Then

                Throw New FormatException(
                    "Enter a whole-number percentage from 0 to 100.")
            End If

            Return value / 100D
        End Function

        Public Function LoadGradePercentages(
    departmentId As String) As List(Of RuleRow)

            Const sql As String =
                "SELECT `Component`, `Percentage` " &
                "FROM `gradepercentage` " &
                "WHERE `DepartmentID` = @DepartmentID " &
                "ORDER BY `Component`;"

            Dim rules As New List(Of RuleRow)()

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    command.Parameters.AddWithValue("@DepartmentID", departmentId)

                    connection.Open()

                    Using reader = command.ExecuteReader()
                        While reader.Read()
                            rules.Add(
                                New RuleRow With {
                                    .Aspect = Value(reader, "Component"),
                                    .Percentage = WholePercentageFromDatabase(
                                        Value(reader, "Percentage")) & "%"
                                })
                        End While
                    End Using
                End Using
            End Using

            Return rules
        End Function

        Public Sub UpdateGradePercentage(
            departmentId As String,
            component As String,
            percentage As Decimal)

            If percentage < 0D OrElse
               percentage > 100D OrElse
               percentage <> Decimal.Truncate(percentage) Then
                Throw New ArgumentOutOfRangeException(
                    NameOf(percentage),
                    "Each percentage must be between 0% and 100%.")
            End If

            Const totalSql As String =
                "SELECT COALESCE(SUM(`Percentage`), 0) " &
                "FROM `gradepercentage` " &
                "WHERE `DepartmentID` = @DepartmentID " &
                "AND `Component` <> @Component;"

            Const updateSql As String =
                "UPDATE `gradepercentage` " &
                "SET `Percentage` = @Percentage " &
                "WHERE `DepartmentID` = @DepartmentID " &
                "AND `Component` = @Component;"

            Using connection As New MySqlConnection(ConnectionString)
                connection.Open()

                Using command As New MySqlCommand(totalSql, connection)
                    command.Parameters.AddWithValue("@DepartmentID", departmentId)
                    command.Parameters.AddWithValue("@Component", component)

                    Dim otherTotal =
                        Convert.ToDecimal(command.ExecuteScalar())

                    If otherTotal + DatabasePercentageFromWhole(
                        percentage.ToString(CultureInfo.InvariantCulture)) <> 1D Then
                        Throw New InvalidOperationException(
                            "The total percentage for the department must equal exactly 100%.")
                    End If
                End Using

                Using command As New MySqlCommand(updateSql, connection)
                    command.Parameters.AddWithValue("@DepartmentID", departmentId)
                    command.Parameters.AddWithValue("@Component", component)
                    command.Parameters.AddWithValue(
                        "@Percentage",
                        DatabasePercentageFromWhole(
                            percentage.ToString(CultureInfo.InvariantCulture)))

                    If command.ExecuteNonQuery() = 0 Then
                        Throw New InvalidOperationException(
                            "The grading component was not found.")
                    End If
                End Using
            End Using
        End Sub

        Public Sub UpdateGradePercentages(
            departmentId As String,
            percentages As IEnumerable(Of RuleRow))

            Dim parsed As New List(Of Tuple(Of String, Decimal))()
            Dim total As Decimal = 0D

            For Each rule In percentages
                Dim databasePercentage = DatabasePercentageFromWhole(rule.Percentage)
                parsed.Add(Tuple.Create(rule.Aspect, databasePercentage))
                total += databasePercentage * 100D
            Next

            If total <> 100D Then
                Throw New InvalidOperationException(
                    "The total percentage for the department must equal exactly 100%.")
            End If

            Const updateSql As String =
                "UPDATE `gradepercentage` " &
                "SET `Percentage` = @Percentage " &
                "WHERE `DepartmentID` = @DepartmentID " &
                "AND `Component` = @Component;"

            Using connection As New MySqlConnection(ConnectionString)
                connection.Open()

                Using transaction = connection.BeginTransaction()
                    Try
                        For Each percentageValue In parsed
                            Using command As New MySqlCommand(updateSql, connection, transaction)
                                command.Parameters.AddWithValue("@Percentage", percentageValue.Item2)
                                command.Parameters.AddWithValue("@DepartmentID", departmentId)
                                command.Parameters.AddWithValue("@Component", percentageValue.Item1)

                                If command.ExecuteNonQuery() = 0 Then
                                    Throw New InvalidOperationException(
                                        "The grading component was not found.")
                                End If
                            End Using
                        Next

                        transaction.Commit()
                    Catch
                        transaction.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        Public Function LoadProfessorItemizedGrades(
    classId As String) As List(Of ProfessorItemizedGradeRow)

            Const sql As String =
        "SELECT " &
        "`ClassID`, `Term`, `Component`, `StudentID`, " &
        "`StudentName`, `ClassItemID`, `ItemName`, " &
        "`HighestScore`, `AcquiredScore`, `ComponentWeight` " &
        "FROM `vw_professoritemizedgrades` " &
        "WHERE `ClassID` = @ClassID " &
        "ORDER BY `Component`, `ClassItemID`, `StudentName`;"


            Dim rows As New List(Of ProfessorItemizedGradeRow)()

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    command.Parameters.AddWithValue("@ClassID", classId)

                    connection.Open()

                    Using reader = command.ExecuteReader()
                        While reader.Read()
                            rows.Add(
                        New ProfessorItemizedGradeRow With {
                            .ClassID = Value(reader, "ClassID"),
                            .Term = Value(reader, "Term"),
                            .Component = Value(reader, "Component"),
                            .StudentID = Value(reader, "StudentID"),
                            .StudentName = Value(reader, "StudentName"),
                            .ClassItemID = Value(reader, "ClassItemID"),
                            .ItemName = Value(reader, "ItemName"),
                            .HighestScore = Value(reader, "HighestScore"),
                            .AcquiredScore = Value(reader, "AcquiredScore"),
                            .ComponentWeight = Value(reader, "ComponentWeight")
                        })
                        End While
                    End Using
                End Using
            End Using

            Return rows
        End Function

        Public Function LoadProfessorClasses(
            professorId As String) As List(Of ClassRow)

            Const sql As String =
                "SELECT `ClassID`, `CourseCode`, `Section`, " &
                "`SchoolYear`, `Semester` " &
                "FROM `classoffering` " &
                "WHERE `ProfessorID` = @ProfessorID " &
                "ORDER BY `SchoolYear`, `Semester`, `CourseCode`, `Section`;"

            Dim classes As New List(Of ClassRow)()

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    command.Parameters.AddWithValue("@ProfessorID", professorId)
                    connection.Open()

                    Using reader = command.ExecuteReader()
                        While reader.Read()
                            classes.Add(
                                New ClassRow With {
                                    .ClassID = Value(reader, "ClassID"),
                                    .Course = Value(reader, "CourseCode"),
                                    .Section = Value(reader, "Section"),
                                    .SchoolYear = Value(reader, "SchoolYear"),
                                    .Semester = Value(reader, "Semester")
                                })
                        End While
                    End Using
                End Using
            End Using

            Return classes
        End Function

        Public Function LoadProfessorAttendance(
            classId As String) As List(Of ProfessorAttendanceRow)

            EnsureStudentAttendanceRows(classId)

            Const sql As String =
                "SELECT ca.`ClassID`, s.`StudentID`, " &
                "CONCAT(UPPER(s.`student_Lname`), ', ', UPPER(s.`student_Fname`), " &
                "IF(s.`student_Mname` IS NOT NULL AND s.`student_Mname` <> '', " &
                "CONCAT(' ', UPPER(LEFT(s.`student_Mname`, 1)), '.'), '')) AS `StudentName`, " &
                "ca.`AttendanceID`, DATE_FORMAT(ca.`ClassDate`, '%m/%d/%y') AS `AttendanceDate`, " &
                "ca.`ClassDate` AS `RawClassDate`, COALESCE(sa.`Status`, '') AS `Status` " &
                "FROM `classattendance` ca " &
                "JOIN `studentenrollment` se ON se.`ClassID` = ca.`ClassID` " &
                "JOIN `student` s ON s.`StudentID` = se.`StudentID` " &
                "LEFT JOIN `studentattendance` sa ON sa.`AttendanceID` = ca.`AttendanceID` " &
                "AND sa.`StudentID` = s.`StudentID` " &
                "WHERE ca.`ClassID` = @ClassID " &
                "ORDER BY ca.`ClassDate`, `StudentName`;"

            Dim rows As New List(Of ProfessorAttendanceRow)()

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    command.Parameters.AddWithValue("@ClassID", classId)
                    connection.Open()

                    Using reader = command.ExecuteReader()
                        While reader.Read()
                            rows.Add(
                                New ProfessorAttendanceRow With {
                                    .ClassID = Value(reader, "ClassID"),
                                    .StudentID = Value(reader, "StudentID"),
                                    .StudentName = Value(reader, "StudentName"),
                                    .AttendanceID = Value(reader, "AttendanceID"),
                                    .AttendanceDate = Value(reader, "AttendanceDate"),
                                    .RawClassDate = Value(reader, "RawClassDate"),
                                    .Status = Value(reader, "Status")
                                })
                        End While
                    End Using
                End Using
            End Using

            Return rows
        End Function

        Private Sub EnsureStudentAttendanceRows(classId As String)
            If Not _attendanceSchemaReady Then
                Const alterSql As String =
                    "ALTER TABLE `studentattendance` " &
                    "MODIFY `Status` ENUM('Present','Absent','Late','Excused') NULL;"

                Db.Execute(alterSql)
                _attendanceSchemaReady = True
            End If

            Const insertSql As String =
                "INSERT INTO `studentattendance` (`AttendanceID`, `StudentID`, `Status`) " &
                "SELECT ca.`AttendanceID`, se.`StudentID`, NULL " &
                "FROM `classattendance` ca " &
                "JOIN `studentenrollment` se ON se.`ClassID` = ca.`ClassID` " &
                "WHERE ca.`ClassID` = @ClassID " &
                "ON DUPLICATE KEY UPDATE `Status` = `studentattendance`.`Status`;"

            Db.Execute(insertSql, ("@ClassID", classId))
        End Sub

        Public Sub UpdateProfessorAttendance(
            attendanceId As String,
            studentId As String,
            status As String)

            If IsProfessorGradeEditingLocked(Session.UserId) Then
                Throw New InvalidOperationException("Grade editing is locked for you.")
            End If

            Const sql As String =
                "INSERT INTO `studentattendance` " &
                "(`AttendanceID`, `StudentID`, `Status`) " &
                "VALUES (@AttendanceID, @StudentID, @Status) " &
                "ON DUPLICATE KEY UPDATE " &
                "`Status` = VALUES(`Status`);"

            Using connection As New MySqlConnection(ConnectionString)
                Using command As New MySqlCommand(sql, connection)
                    command.Parameters.AddWithValue("@AttendanceID", attendanceId)
                    command.Parameters.AddWithValue("@StudentID", studentId)
                    command.Parameters.AddWithValue(
                        "@Status",
                        If(String.IsNullOrWhiteSpace(status), DBNull.Value, status))
                    connection.Open()
                    command.ExecuteNonQuery()
                End Using
            End Using

            RecalculateStudentGradesFromAttendance(attendanceId, studentId)
        End Sub

        Public Sub UpdateProfessorItemizedGrade(
            classItemId As String,
            studentId As String,
            acquiredScore As String)

            If IsProfessorGradeEditingLocked(Session.UserId) Then
                Throw New InvalidOperationException("Grade editing is locked for you.")
            End If

            Const upsertSql As String =
                "INSERT INTO `classscore` " &
                "(`StudentID`, `ClassItemID`, `AcquiredScore`) " &
                "VALUES (@StudentID, @ClassItemID, @AcquiredScore) " &
                "ON DUPLICATE KEY UPDATE " &
                "`AcquiredScore` = VALUES(`AcquiredScore`);"

            Dim score As Object = DBNull.Value
            Dim parsedScore As Integer

            If Not String.IsNullOrWhiteSpace(acquiredScore) Then
                If Not Integer.TryParse(
                    acquiredScore.Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    parsedScore) Then

                    Throw New FormatException(
                        "Enter a valid score or leave the field blank.")
                End If

                If parsedScore < 0 Then
                    Throw New ArgumentOutOfRangeException(
                        NameOf(acquiredScore),
                        "The score cannot be negative.")
                End If

                score = parsedScore
            End If

            Using connection As New MySqlConnection(ConnectionString)
                connection.Open()

                Using scoreCommand As New MySqlCommand(upsertSql, connection)
                    scoreCommand.Parameters.AddWithValue("@ClassItemID", classItemId)
                    scoreCommand.Parameters.AddWithValue("@StudentID", studentId)
                    scoreCommand.Parameters.AddWithValue("@AcquiredScore", score)
                    scoreCommand.ExecuteNonQuery()
                End Using
            End Using

            RecalculateStudentGradesFromClassItem(classItemId, studentId)
        End Sub

        Public Sub SynchronizeProfessorGrades(professorId As String)
            Const sql As String =
                "SELECT DISTINCT co.ClassID, se.StudentID " &
                "FROM classoffering co " &
                "JOIN studentenrollment se ON se.ClassID = co.ClassID " &
                "WHERE co.ProfessorID = @ProfessorID;"

            Dim students = Db.Query(sql,
                Function(reader) New GradeSyncRow With {
                    .ClassID = reader.Str("ClassID"),
                    .StudentID = reader.Str("StudentID")
                },
                ("@ProfessorID", professorId))

            For Each student In students
                RecalculateStudentGrades(student.ClassID, student.StudentID)
            Next
        End Sub

        Private Sub RecalculateStudentGradesFromAttendance(attendanceId As String, studentId As String)
            Const sql As String =
                "SELECT `ClassID` FROM `classattendance` WHERE `AttendanceID` = @AttendanceID LIMIT 1;"

            Dim classId = Db.Query(sql,
                Function(reader) reader.Str("ClassID"),
                ("@AttendanceID", attendanceId)).FirstOrDefault()

            If Not String.IsNullOrWhiteSpace(classId) Then RecalculateStudentGrades(classId, studentId)
        End Sub

        Private Sub RecalculateStudentGradesFromClassItem(classItemId As String, studentId As String)
            Const sql As String =
                "SELECT `ClassID` FROM `classitem` WHERE `ClassItemID` = @ClassItemID LIMIT 1;"

            Dim classId = Db.Query(sql,
                Function(reader) reader.Str("ClassID"),
                ("@ClassItemID", classItemId)).FirstOrDefault()

            If Not String.IsNullOrWhiteSpace(classId) Then RecalculateStudentGrades(classId, studentId)
        End Sub

        Private Sub RecalculateStudentGrades(classId As String, studentId As String)
            Dim midterm = CalculateTermGrade(classId, studentId, "Midterm")
            Dim finals = CalculateTermGrade(classId, studentId, "Finals")

            Const gradeSql As String =
                "INSERT INTO `studentcoursegrade` " &
                "(`StudentID`, `ClassID`, `MidtermGrade`, `FinalGrade`) " &
                "VALUES (@StudentID, @ClassID, @MidtermGrade, @FinalGrade) " &
                "ON DUPLICATE KEY UPDATE " &
                "`MidtermGrade` = VALUES(`MidtermGrade`), " &
                "`FinalGrade` = VALUES(`FinalGrade`);"

            Db.Execute(gradeSql,
                ("@StudentID", studentId),
                ("@ClassID", classId),
                ("@MidtermGrade", midterm),
                ("@FinalGrade", finals))

            Dim overall = (midterm + finals) / 2D
            Dim gwa = Decimal.Parse(
                GradesService.CalculateGwa(CDbl(overall)),
                CultureInfo.InvariantCulture)

            Const classSql As String =
                "SELECT `SchoolYear`, `Semester` FROM `classoffering` " &
                "WHERE `ClassID` = @ClassID LIMIT 1;"

            Dim classInfo = Db.Query(classSql,
                Function(reader) New ClassTerm With {
                    .SchoolYear = reader.Str("SchoolYear"),
                    .Semester = reader.Str("Semester")
                },
                ("@ClassID", classId)).FirstOrDefault()

            If classInfo Is Nothing Then Return

            Const overallSql As String =
                "INSERT INTO `studentgrades` (`ClassID`, `StudentID`, `SemGrade`) " &
                "VALUES (@ClassID, @StudentID, @SemGrade) " &
                "ON DUPLICATE KEY UPDATE `SemGrade` = VALUES(`SemGrade`);"

            Db.Execute(overallSql,
                ("@ClassID", classId),
                ("@StudentID", studentId),
                ("@SemGrade", gwa))

            Const gwaSql As String =
                "INSERT INTO `studentgwa` (`StudentID`, `SchoolYear`, `Semester`, `GWA`) " &
                "VALUES (@StudentID, @SchoolYear, @Semester, @GWA) " &
                "ON DUPLICATE KEY UPDATE `GWA` = VALUES(`GWA`);"

            Db.Execute(gwaSql,
                ("@StudentID", studentId),
                ("@SchoolYear", classInfo.SchoolYear),
                ("@Semester", classInfo.Semester),
                ("@GWA", gwa))
        End Sub

        Private Function CalculateTermGrade(classId As String, studentId As String, term As String) As Decimal
            Const sql As String =
                "SELECT gp.`Component`, gp.`Percentage`, " &
                "CASE WHEN gp.`Component` = 'Attendance' THEN " &
                "(SELECT COALESCE(SUM(CASE WHEN sa.`Status` IN ('Present', 'PRESENT', 'P') THEN 1 ELSE 0 END), 0) " &
                "FROM `classattendance` ca LEFT JOIN `studentattendance` sa " &
                "ON sa.`AttendanceID` = ca.`AttendanceID` AND sa.`StudentID` = @StudentID " &
                "WHERE ca.`ClassID` = @ClassID) " &
                "ELSE (SELECT COALESCE(SUM(cs.`AcquiredScore`), 0) FROM `classitem` ci " &
                "LEFT JOIN `classscore` cs ON cs.`ClassItemID` = ci.`ClassItemID` AND cs.`StudentID` = @StudentID " &
                "WHERE ci.`ClassID` = @ClassID AND ci.`Term` = @Term AND ci.`Component` = gp.`Component`) END AS AcquiredScore, " &
                "CASE WHEN gp.`Component` = 'Attendance' THEN " &
                "(SELECT COUNT(*) FROM `classattendance` WHERE `ClassID` = @ClassID) " &
                "ELSE (SELECT COALESCE(SUM(`HighestScore`), 0) FROM `classitem` " &
                "WHERE `ClassID` = @ClassID AND `Term` = @Term AND `Component` = gp.`Component`) END AS HighestScore " &
                "FROM `gradepercentage` gp JOIN `student` s ON s.`StudentID` = @StudentID " &
                "JOIN `program` p ON p.`ProgramID` = s.`ProgramID` AND p.`DepartmentID` = gp.`DepartmentID`;"

            Dim total As Decimal
            Dim components = Db.Query(sql,
                Function(reader) New ComponentGrade With {
                    .Percentage = Decimal.Parse(reader.Num("Percentage"), CultureInfo.InvariantCulture),
                    .Acquired = Decimal.Parse(reader.Num("AcquiredScore"), CultureInfo.InvariantCulture),
                    .Highest = Decimal.Parse(reader.Num("HighestScore"), CultureInfo.InvariantCulture)
                },
                ("@ClassID", classId),
                ("@StudentID", studentId),
                ("@Term", term))

            For Each component In components
                If component.Highest > 0D Then
                    total += (component.Acquired / component.Highest) * 100D * component.Percentage
                End If
            Next

            Return Decimal.Round(total, 2)
        End Function

        Private Class ComponentGrade
            Public Property Percentage As Decimal
            Public Property Acquired As Decimal
            Public Property Highest As Decimal
        End Class

        Private Class ClassTerm
            Public Property SchoolYear As String = ""
            Public Property Semester As String = ""
        End Class

        Private Class GradeSyncRow
            Public Property ClassID As String = ""
            Public Property StudentID As String = ""
        End Class
    End Module

    Public Class ProfessorItemizedGradeRow
        Implements INotifyPropertyChanged

        Private _acquiredScore As String = ""

        Public Property ClassID As String = ""
        Public Property Term As String = ""
        Public Property Component As String = ""
        Public Property StudentID As String = ""
        Public Property StudentName As String = ""
        Public Property ClassItemID As String = ""
        Public Property ItemName As String = ""
        Public Property HighestScore As String = ""
        Public Property AcquiredScore As String
            Get
                Return _acquiredScore
            End Get
            Set(value As String)
                If _acquiredScore = value Then Return
                _acquiredScore = value
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(AcquiredScore)))
            End Set
        End Property
        Public Property ComponentWeight As String = ""
        Public Property AttendanceID As String = ""
        Public Property IsAttendance As Boolean

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    End Class


End Namespace

