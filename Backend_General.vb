Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports MySqlConnector
Imports SidhawanForProfsAndAdmins

Namespace SidhawanForProfsAndAdmins

    ' Storage for user information in every session
    Public NotInheritable Class Session
        Private Sub New()
        End Sub

        Private Shared _userId As String = String.Empty
        Private Shared _userName As String = String.Empty

        Public Shared ReadOnly Property UserId As String
            Get
                Return _userId
            End Get
        End Property

        Public Shared ReadOnly Property UserName As String
            Get
                Return _userName
            End Get
        End Property

        Public Shared ReadOnly Property IsLoggedIn As Boolean
            Get
                Return Not String.IsNullOrEmpty(_userId)
            End Get
        End Property

        Friend Shared Sub SignIn(id As String, name As String)
            _userId = id
            _userName = name
        End Sub

        Public Shared Sub SignOut()
            _userId = String.Empty
            _userName = String.Empty
        End Sub
    End Class

    ' Various Login Authentication related methods
    Public NotInheritable Class AuthManager
        Private Sub New()
        End Sub

        Public Enum LoginResult
            Success
            InvalidCredentials
            LockedOut
            ServerError
        End Enum

        Private Const MaxAttempts As Integer = 5
        Private Shared _failedAttempts As Integer
        Private Shared _lockedUntil As DateTime = DateTime.MinValue

        ' User login validation
        Public Shared Function ValidateUserLogin(userIdInput As String, passwordInput As String, expectedRole As String) As LoginResult
            If DateTime.UtcNow < _lockedUntil Then Return LoginResult.LockedOut

            If String.IsNullOrWhiteSpace(userIdInput) OrElse String.IsNullOrEmpty(passwordInput) Then
                Return LoginResult.InvalidCredentials
            End If

            Dim id As String = userIdInput.Trim()

            Try
                ' Parameterized query: the input is never concatenated into the SQL.
                Dim rows = Db.Query(
                    "SELECT Lname, PasswordHash, Role, IsAdmin FROM employee WHERE UserID = @id LIMIT 1",
                    Function(r) (LastName:=r.Str("Lname"),
                                 Hash:=r.Str("PasswordHash"),
                                 Role:=r.Str("Role"),
                                 IsAdmin:=r.Str("IsAdmin")),
                    ("@id", id))

                ' Ordinal = case-sensitive match
                If rows.Count > 0 AndAlso
                   String.Equals(rows(0).Hash, passwordInput, StringComparison.Ordinal) AndAlso
                   ((String.Equals(expectedRole, "Admin", StringComparison.OrdinalIgnoreCase) AndAlso
                     (rows(0).IsAdmin = "1" OrElse
                      String.Equals(rows(0).IsAdmin, "true", StringComparison.OrdinalIgnoreCase))) OrElse
                    (String.Equals(expectedRole, "Professor", StringComparison.OrdinalIgnoreCase) AndAlso
                     String.Equals(rows(0).Role, "Professor", StringComparison.OrdinalIgnoreCase))) Then
                    _failedAttempts = 0
                    Session.SignIn(id, rows(0).LastName)
                    Return LoginResult.Success
                End If

                _failedAttempts += 1
                If _failedAttempts >= MaxAttempts Then
                    _lockedUntil = DateTime.UtcNow.AddMinutes(1)
                    _failedAttempts = 0
                End If
                Return LoginResult.InvalidCredentials

            Catch ex As Exception
                System.Diagnostics.Trace.TraceError("Login failed: " & ex.ToString())
                System.Windows.MessageBox.Show("Could not reach the server. Please try again later.")
                Return LoginResult.ServerError
            End Try
        End Function

        Public Enum ChangeResult
            Success
            NotLoggedIn
            WrongCurrent
            WeakPassword
            SameAsOld
            ServerError
        End Enum

        ' Change Password method to overwrite database content
        Public Shared Function ChangePassword(currentPw As String, newPw As String) As ChangeResult
            If Not Session.IsLoggedIn Then Return ChangeResult.NotLoggedIn
            If Not Passwords.MeetsPolicy(newPw) Then Return ChangeResult.WeakPassword

            Try
                Dim rows = Db.Query(
                    "SELECT PasswordHash FROM employee WHERE UserID = @id LIMIT 1",
                    Function(r) r.Str("PasswordHash"),
                    ("@id", Session.UserId))

                If rows.Count = 0 OrElse Not String.Equals(rows(0), currentPw, StringComparison.Ordinal) Then
                    Return ChangeResult.WrongCurrent
                End If
                If String.Equals(newPw, currentPw, StringComparison.Ordinal) Then Return ChangeResult.SameAsOld

                Dim changed As Integer = Db.Execute(
                    "UPDATE employee SET PasswordHash = @pw WHERE UserID = @id",
                    ("@pw", newPw),
                    ("@id", Session.UserId))

                Return If(changed = 1, ChangeResult.Success, ChangeResult.ServerError)
            Catch ex As Exception
                System.Diagnostics.Trace.TraceError("Password change failed: " & ex.ToString())
                Return ChangeResult.ServerError
            End Try
        End Function
    End Class

    ' Password rules
    Public NotInheritable Class Passwords
        Private Sub New()
        End Sub

        Private Shared Function AnyChar(pw As String, test As Func(Of Char, Boolean)) As Boolean
            Return pw IsNot Nothing AndAlso pw.Any(test)
        End Function

        Public Shared Function HasLength(pw As String) As Boolean
            Return pw IsNot Nothing AndAlso pw.Length >= 8 AndAlso pw.Length <= 16
        End Function
        Public Shared Function HasUpper(pw As String) As Boolean
            Return AnyChar(pw, Function(c) Char.IsUpper(c))
        End Function
        Public Shared Function HasLower(pw As String) As Boolean
            Return AnyChar(pw, Function(c) Char.IsLower(c))
        End Function
        Public Shared Function HasDigit(pw As String) As Boolean
            Return AnyChar(pw, Function(c) Char.IsDigit(c))
        End Function
        Public Shared Function HasSpecial(pw As String) As Boolean
            Return AnyChar(pw, Function(c) Char.IsPunctuation(c) OrElse Char.IsSymbol(c))
        End Function
        Public Shared Function MeetsPolicy(pw As String) As Boolean
            Return HasLength(pw) AndAlso HasUpper(pw) AndAlso HasLower(pw) AndAlso HasDigit(pw) AndAlso HasSpecial(pw)
        End Function
    End Class



    ' Grade Service related methods
    Public NotInheritable Class GradesService
        Private Sub New()
        End Sub

        Private Shared _warned As Boolean

        Public Shared Function CalculateGwa(gradeAverage As Double) As String
            Dim gwa As Double

            If gradeAverage >= 97 Then
                gwa = 1.0
            ElseIf gradeAverage >= 94 Then
                gwa = 1.25
            ElseIf gradeAverage >= 91 Then
                gwa = 1.5
            ElseIf gradeAverage >= 88 Then
                gwa = 1.75
            ElseIf gradeAverage >= 85 Then
                gwa = 2.0
            ElseIf gradeAverage >= 82 Then
                gwa = 2.25
            ElseIf gradeAverage >= 79 Then
                gwa = 2.5
            ElseIf gradeAverage >= 76 Then
                gwa = 2.75
            ElseIf gradeAverage = 75 Then
                gwa = 3.0
            Else
                gwa = 5.0
            End If

            Return gwa.ToString("0.00", CultureInfo.InvariantCulture)
        End Function

        Public Shared Function CalculateOverallGrade(midtermGrade As Double, finalGrade As Double) As String
            Dim overallGrade = (midtermGrade + finalGrade) / 2.0
            Return overallGrade.ToString("0.00", CultureInfo.InvariantCulture)
        End Function

        Private Shared Function Cwa(rows As IEnumerable(Of GradeRow)) As String
            Dim points As Double = 0
            Dim units As Double = 0
            For Each row In rows
                Dim grade As Double
                Dim unit As Double
                If Double.TryParse(row.SemGrade, NumberStyles.Any, CultureInfo.InvariantCulture, grade) AndAlso
                   Double.TryParse(row.Units, NumberStyles.Any, CultureInfo.InvariantCulture, unit) Then
                    points += grade * unit
                    units += unit
                End If
            Next
            Return If(units > 0, (points / units).ToString("0.00", CultureInfo.InvariantCulture), "")
        End Function

        Private Shared Sub Safe(work As Action)
            Try
                work()
            Catch ex As Exception
                System.Diagnostics.Trace.TraceError("Grades load failed: " & ex.ToString())
                If _warned Then Return
                _warned = True
                System.Windows.MessageBox.Show("Could not load your grades from the server. Please try again later.")
            End Try
        End Sub
    End Class
End Namespace