Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports MySqlConnector
Imports SidhawanForProfsAndAdmins

' Loading of Profile Information related methods
Public NotInheritable Class ProfileService
    Private Sub New()
    End Sub

    Private Const LoadSql As String =
        "SELECT " &
        "    EmployeeNumber," &
        "    Fname," &
        "    Mname," &
        "    Lname," &
        "    Suffix," &
        "    DATE_FORMAT(DOB, '%M %e, %Y') AS DOB," &
        "    Sex," &
        "    COALESCE(ContactNumber, '') AS ContactNumber," &
        "    COALESCE(EmailAddress, '') AS EmailAddress," &
        "    College," &
        "    Position " &
        "FROM vw_professorprofile WHERE EmployeeNumber = @id LIMIT 1"
    Public Shared Sub LoadProfile()
        If Not Session.IsLoggedIn Then Return
        Dim p = LoadPerson(Session.UserId)
        If p IsNot Nothing Then Data.Prof = p
    End Sub
    Public Shared Function LoadPerson(employeeNumber As String) As Person
        If String.IsNullOrWhiteSpace(employeeNumber) Then Return Nothing
        Try
            Dim rows = Db.Query(LoadSql,
                Function(r)
                    Dim p As New Person()
                    p.Id = r.Str("EmployeeNumber")
                    p.FirstName = r.Str("Fname")
                    p.MiddleName = r.Str("Mname")
                    p.LastName = r.Str("Lname")
                    p.Suffix = r.Str("Suffix")
                    p.BirthDate = r.Str("DOB")
                    p.Sex = r.Str("Sex")
                    p.ContactNumber = r.Str("ContactNumber")
                    p.Email = r.Str("EmailAddress")

                    AddAcademic(p, "College", r.Str("College"))
                    AddAcademic(p, "Position", r.Str("Position"))
                    Return p
                End Function,
                ("@id", employeeNumber.Trim()))

            Return If(rows.Count > 0, rows(0), Nothing)
        Catch ex As Exception
            System.Diagnostics.Trace.TraceError("Profile load failed: " & ex.ToString())
            System.Windows.MessageBox.Show("Could not load your professor profile. Please try again later.")
            Return Nothing
        End Try
    End Function

    Private Shared Sub AddAcademic(p As Person, label As String, value As String)
        p.Academic.Add(New Field(label, value))
    End Sub
End Class

Public NotInheritable Class ProfessorStudentService
    Private Sub New()
    End Sub

    Private Const StudentsSql As String =
        "SELECT SchoolYear, Semester, CourseCode, Section, " &
        "StudentNo, StudentName, Status, MidtermGrade, FinalGrade " &
        "FROM vw_professorstudents " &
        "WHERE ProfessorID = @professorId " &
        "AND (@schoolYear = '' OR SchoolYear = @schoolYear) " &
        "AND (@semester = '' OR Semester = @semester) " &
        "AND (@course = '' OR CourseCode = @course) " &
        "AND (@section = '' OR Section = @section) " &
        "ORDER BY SchoolYear DESC, Semester, CourseCode, Section, StudentNo"

    Public Shared Sub LoadStudentFilters()
        If Not Session.IsLoggedIn Then Return

        Try
            Dim rows = QueryStudents("", "", "", "")
            Replace(Data.SchoolYears, rows.Select(Function(r) r.SchoolYear).Distinct())
            Replace(Data.Semesters, rows.Select(Function(r) r.Semester).Distinct())
            Replace(Data.Courses, rows.Select(Function(r) r.CourseCode).Distinct())
            Replace(Data.Sections, rows.Select(Function(r) r.Section).Distinct())
            Replace(Data.ClassStudents, rows)
        Catch ex As Exception
            System.Diagnostics.Trace.TraceError("Professor students load failed: " & ex.ToString())
            System.Windows.MessageBox.Show("Could not load your students. Please try again later.")
        End Try
    End Sub

    Public Shared Sub LoadStudents(schoolYear As String, semester As String, course As String, section As String)
        If Not Session.IsLoggedIn Then Return

        Try
            Replace(Data.ClassStudents, QueryStudents(schoolYear, semester, course, section))
        Catch ex As Exception
            System.Diagnostics.Trace.TraceError("Professor students filter failed: " & ex.ToString())
            System.Windows.MessageBox.Show("Could not load the selected students. Please try again later.")
        End Try
    End Sub

    Public Shared Sub LoadSummaryFilters()
        If Not Session.IsLoggedIn Then Return

        Try
            Database.SynchronizeProfessorGrades(Session.UserId)
            Dim rows = QuerySummary("", "", "")
            Replace(Data.SchoolYears, rows.Select(Function(r) r.SchoolYear).Distinct())
            Replace(Data.Sections, rows.Select(Function(r) r.Section).Distinct())
            Replace(Data.Courses, rows.Select(Function(r) r.CourseCode).Distinct())
            LoadSummary("", "", "")
        Catch ex As Exception
            System.Diagnostics.Trace.TraceError("Professor summary filters load failed: " & ex.ToString())
            System.Windows.MessageBox.Show("Could not load your summary records. Please try again later.")
        End Try
    End Sub

    Public Shared Sub LoadSummary(schoolYear As String, section As String, course As String)
        If Not Session.IsLoggedIn Then Return

        Try
            Dim rows = QuerySummary(schoolYear, section, course)
            Data.SummarySheet.Rows.Clear()
            Dim firstRow = rows.FirstOrDefault()
            Data.SummarySheet.Rows.Add(MatrixRow.Create(
                "Grading Percentage:",
                If(firstRow Is Nothing, "", firstRow.AttendancePercentage),
                If(firstRow Is Nothing, "", firstRow.QuizzesPercentage),
                If(firstRow Is Nothing, "", firstRow.ClassworkPercentage),
                If(firstRow Is Nothing, "", firstRow.MajorExamPercentage),
                "", "", "", "", ""))
            Data.SummarySheet.Rows(0).Bold = True
            Data.SummarySheet.Rows.Add(MatrixRow.Create(
                "Highest Possible Score:",
                If(firstRow Is Nothing, "", firstRow.AttendanceHighest),
                If(firstRow Is Nothing, "", firstRow.QuizzesHighest),
                If(firstRow Is Nothing, "", firstRow.ClassworkHighest),
                If(firstRow Is Nothing, "", firstRow.MajorExamHighest),
                "", "", "", "", ""))
            Data.SummarySheet.Rows(1).Bold = True
            For Each row In rows
                Data.SummarySheet.Rows.Add(MatrixRow.Create(
                    row.StudentName,
                    row.Attendance,
                    row.WrittenWork,
                    row.PerformanceTask,
                    row.MajorExam,
                    row.MidtermGrade,
                    row.FinalGrade,
                    row.OverallGrade,
                    row.Gwa,
                    row.Remarks))
            Next
        Catch ex As Exception
            System.Diagnostics.Trace.TraceError("Professor summary load failed: " & ex.ToString())
            System.Windows.MessageBox.Show("Could not load the selected summary records. Please try again later.")
        End Try
    End Sub

    Public Shared Function LoadSummaryForClass(classId As String) As List(Of ProfessorSummaryRow)
        If String.IsNullOrWhiteSpace(classId) Then
            Return New List(Of ProfessorSummaryRow)()
        End If

        Return QuerySummary("", "", "", classId)
    End Function

    Private Shared Function QueryStudents(schoolYear As String, semester As String, course As String, section As String) As List(Of StudentRow)
        Return Db.Query(StudentsSql,
            Function(r)
                Return New StudentRow With {
                    .StudentNo = r.Str("StudentNo"),
                    .Name = r.Str("StudentName"),
                    .Status = r.Str("Status"),
                    .MidtermGrade = r.Str("MidtermGrade"),
                    .FinalGrade = r.Str("FinalGrade"),
                    .SchoolYear = r.Str("SchoolYear"),
                    .Semester = r.Str("Semester"),
                    .CourseCode = r.Str("CourseCode"),
                    .Section = r.Str("Section")
                }
            End Function,
            ("@professorId", Session.UserId),
            ("@schoolYear", If(schoolYear, "")),
            ("@semester", If(semester, "")),
            ("@course", If(course, "")),
            ("@section", If(section, "")))
    End Function

    Private Shared Function QuerySummary(
        schoolYear As String,
        section As String,
        course As String,
        Optional classId As String = "") As List(Of ProfessorSummaryRow)
        Const sql As String =
            "SELECT co.SchoolYear, co.Section, co.CourseCode, " &
            "CONCAT(UPPER(s.student_Lname), ', ', UPPER(s.student_Fname), " &
            "IF(s.student_Mname IS NOT NULL AND s.student_Mname <> '', " &
            "CONCAT(' ', UPPER(LEFT(s.student_Mname, 1)), '.'), '')) AS StudentName, " &
            "COALESCE((SELECT COUNT(*) FROM classattendance ca " &
            "LEFT JOIN studentattendance sa ON sa.AttendanceID = ca.AttendanceID " &
            "AND sa.StudentID = s.StudentID " &
            "WHERE ca.ClassID = co.ClassID " &
            "AND sa.Status IN ('Present', 'PRESENT', 'P')), 0) AS Attendance, " &
            "COALESCE((SELECT SUM(cs.AcquiredScore) FROM classitem ci " &
            "LEFT JOIN classscore cs ON cs.ClassItemID = ci.ClassItemID " &
            "AND cs.StudentID = s.StudentID " &
            "WHERE ci.ClassID = co.ClassID AND ci.Component = 'Quizzes'), 0) AS Quizzes, " &
            "COALESCE((SELECT SUM(cs.AcquiredScore) FROM classitem ci " &
            "LEFT JOIN classscore cs ON cs.ClassItemID = ci.ClassItemID " &
            "AND cs.StudentID = s.StudentID " &
            "WHERE ci.ClassID = co.ClassID AND ci.Component = 'Classwork'), 0) AS Classwork, " &
            "COALESCE((SELECT SUM(cs.AcquiredScore) FROM classitem ci " &
            "LEFT JOIN classscore cs ON cs.ClassItemID = ci.ClassItemID " &
            "AND cs.StudentID = s.StudentID " &
            "WHERE ci.ClassID = co.ClassID AND ci.Component = 'Major Examination'), 0) AS MajorExam, " &
            "COALESCE((SELECT COUNT(*) FROM classattendance ca " &
            "WHERE ca.ClassID = co.ClassID), 0) AS AttendanceHighest, " &
            "COALESCE((SELECT SUM(ci.HighestScore) FROM classitem ci " &
            "WHERE ci.ClassID = co.ClassID AND ci.Component = 'Quizzes'), 0) AS QuizzesHighest, " &
            "COALESCE((SELECT SUM(ci.HighestScore) FROM classitem ci " &
            "WHERE ci.ClassID = co.ClassID AND ci.Component = 'Classwork'), 0) AS ClassworkHighest, " &
            "COALESCE((SELECT SUM(ci.HighestScore) FROM classitem ci " &
            "WHERE ci.ClassID = co.ClassID AND ci.Component = 'Major Examination'), 0) AS MajorExamHighest, " &
            "COALESCE((SELECT CONCAT(ROUND(gp.Percentage * 100, 0), '%') FROM gradepercentage gp " &
            "JOIN program p ON p.DepartmentID = gp.DepartmentID " &
            "WHERE p.ProgramID = s.ProgramID AND gp.Component = 'Attendance' LIMIT 1), '') AS AttendancePercentage, " &
            "COALESCE((SELECT CONCAT(ROUND(gp.Percentage * 100, 0), '%') FROM gradepercentage gp " &
            "JOIN program p ON p.DepartmentID = gp.DepartmentID " &
            "WHERE p.ProgramID = s.ProgramID AND gp.Component = 'Quizzes' LIMIT 1), '') AS QuizzesPercentage, " &
            "COALESCE((SELECT CONCAT(ROUND(gp.Percentage * 100, 0), '%') FROM gradepercentage gp " &
            "JOIN program p ON p.DepartmentID = gp.DepartmentID " &
            "WHERE p.ProgramID = s.ProgramID AND gp.Component = 'Classwork' LIMIT 1), '') AS ClassworkPercentage, " &
            "COALESCE((SELECT CONCAT(ROUND(gp.Percentage * 100, 0), '%') FROM gradepercentage gp " &
            "JOIN program p ON p.DepartmentID = gp.DepartmentID " &
            "WHERE p.ProgramID = s.ProgramID AND gp.Component = 'Major Examination' LIMIT 1), '') AS MajorExamPercentage, " &
            "cg.MidtermGrade, cg.FinalGrade, sg.SemGrade AS StoredGwa, gwa.GWA AS DatabaseGwa, " &
            "se.EnrollmentStatus, " &
            "CASE WHEN se.EnrollmentStatus = 'Dropped' THEN 0 " &
            "WHEN EXISTS (SELECT 1 FROM classitem ci " &
            "LEFT JOIN classscore cs ON cs.ClassItemID = ci.ClassItemID AND cs.StudentID = s.StudentID " &
            "WHERE ci.ClassID = co.ClassID AND (cs.StudentID IS NULL OR cs.AcquiredScore IS NULL)) THEN 0 " &
            "WHEN EXISTS (SELECT 1 FROM classattendance ca " &
            "LEFT JOIN studentattendance sa ON sa.AttendanceID = ca.AttendanceID AND sa.StudentID = s.StudentID " &
            "WHERE ca.ClassID = co.ClassID AND (sa.StudentID IS NULL OR sa.Status IS NULL OR sa.Status = '')) THEN 0 " &
            "ELSE 1 END AS GradeFinished " &
            "FROM classoffering co " &
            "JOIN studentenrollment se ON se.ClassID = co.ClassID " &
            "JOIN student s ON s.StudentID = se.StudentID " &
            "LEFT JOIN studentcoursegrade cg ON cg.ClassID = co.ClassID AND cg.StudentID = s.StudentID " &
            "LEFT JOIN studentgrades sg ON sg.ClassID = co.ClassID AND sg.StudentID = s.StudentID " &
            "LEFT JOIN studentgwa gwa ON gwa.StudentID = s.StudentID " &
            "AND gwa.SchoolYear = co.SchoolYear AND gwa.Semester = co.Semester " &
            "WHERE (@professorId = '' OR co.ProfessorID = @professorId) " &
            "AND (@classId = '' OR co.ClassID = @classId) " &
            "AND (@schoolYear = '' OR co.SchoolYear = @schoolYear) " &
            "AND (@section = '' OR co.Section = @section) " &
            "AND (@course = '' OR co.CourseCode = @course) " &
            "ORDER BY co.SchoolYear DESC, co.Section, co.CourseCode, StudentName"

        Return Db.Query(sql,
            Function(r) MapSummaryRow(r),
            ("@professorId", If(String.IsNullOrWhiteSpace(classId), Session.UserId, "")),
            ("@schoolYear", If(schoolYear, "")),
            ("@section", If(section, "")),
            ("@course", If(course, "")),
            ("@classId", If(classId, "")))
    End Function

    Private Shared Function MapSummaryRow(r As MySqlConnector.MySqlDataReader) As ProfessorSummaryRow
        Dim midterm = If(String.IsNullOrWhiteSpace(r.Str("MidtermGrade")), "0.00", r.Str("MidtermGrade"))
        Dim final = If(String.IsNullOrWhiteSpace(r.Str("FinalGrade")), "0.00", r.Str("FinalGrade"))
        Dim overall = CalculateOverall(midterm, final)
        Dim gwa = r.Str("DatabaseGwa")
        If String.IsNullOrWhiteSpace(gwa) Then gwa = CalculateGwa(overall)

        Return New ProfessorSummaryRow With {
            .SchoolYear = r.Str("SchoolYear"),
            .Section = r.Str("Section"),
            .CourseCode = r.Str("CourseCode"),
            .StudentName = r.Str("StudentName"),
            .Attendance = r.Str("Attendance"),
            .WrittenWork = r.Str("Quizzes"),
            .PerformanceTask = r.Str("Classwork"),
            .MajorExam = r.Str("MajorExam"),
            .AttendanceHighest = r.Str("AttendanceHighest"),
            .QuizzesHighest = r.Str("QuizzesHighest"),
            .ClassworkHighest = r.Str("ClassworkHighest"),
            .MajorExamHighest = r.Str("MajorExamHighest"),
            .AttendancePercentage = r.Str("AttendancePercentage"),
            .QuizzesPercentage = r.Str("QuizzesPercentage"),
            .ClassworkPercentage = r.Str("ClassworkPercentage"),
            .MajorExamPercentage = r.Str("MajorExamPercentage"),
            .MidtermGrade = midterm,
            .FinalGrade = final,
            .OverallGrade = overall,
            .Remarks = ProcessRemarks(
                r.Str("EnrollmentStatus"),
                r.Str("GradeFinished") = "1",
                gwa),
            .Gwa = gwa
        }
    End Function

    Private Shared Function ProcessRemarks(
        enrollmentStatus As String,
        gradeFinished As Boolean,
        gwa As String) As String

        If String.Equals(enrollmentStatus, "Dropped", StringComparison.OrdinalIgnoreCase) Then
            Return "Dropped"
        End If

        If Not gradeFinished Then Return "Incomplete"

        Dim gwaValue As Decimal
        If Not Decimal.TryParse(gwa, NumberStyles.Any, CultureInfo.InvariantCulture, gwaValue) Then
            Return "Incomplete"
        End If

        Return If(gwaValue <= 3D, "Passed", "Failed")
    End Function

    Private Shared Function CalculateOverall(midterm As String, final As String) As String
        Dim midtermValue As Double
        Dim finalValue As Double
        If Not Double.TryParse(midterm, NumberStyles.Any, CultureInfo.InvariantCulture, midtermValue) OrElse
           Not Double.TryParse(final, NumberStyles.Any, CultureInfo.InvariantCulture, finalValue) Then
            Return ""
        End If

        Return GradesService.CalculateOverallGrade(midtermValue, finalValue)
    End Function

    Private Shared Function CalculateGwa(overall As String) As String
        Dim overallValue As Double
        If Not Double.TryParse(overall, NumberStyles.Any, CultureInfo.InvariantCulture, overallValue) Then Return ""
        Return GradesService.CalculateGwa(overallValue)
    End Function

    Private Shared Sub Replace(Of T)(target As ICollection(Of T), values As IEnumerable(Of T))
        target.Clear()
        For Each value In values
            target.Add(value)
        Next
    End Sub
End Class

Public Class ProfessorSummaryRow
    Public Property SchoolYear As String = ""
    Public Property Section As String = ""
    Public Property CourseCode As String = ""
    Public Property StudentName As String = ""
    Public Property Attendance As String = ""
    Public Property WrittenWork As String = ""
    Public Property PerformanceTask As String = ""
    Public Property MajorExam As String = ""
    Public Property AttendanceHighest As String = ""
    Public Property QuizzesHighest As String = ""
    Public Property ClassworkHighest As String = ""
    Public Property MajorExamHighest As String = ""
    Public Property AttendancePercentage As String = ""
    Public Property QuizzesPercentage As String = ""
    Public Property ClassworkPercentage As String = ""
    Public Property MajorExamPercentage As String = ""
    Public Property MidtermGrade As String = ""
    Public Property FinalGrade As String = ""
    Public Property OverallGrade As String = ""
    Public Property Remarks As String = ""
    Public Property Gwa As String = ""
End Class