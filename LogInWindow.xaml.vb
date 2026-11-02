Imports System
Imports System.Windows
Imports System.Windows.Controls

Namespace SidhawanForProfsAndAdmins

    Partial Public Class LogInWindow
        Inherits Window

        Public Sub New()
            Me.InitializeComponent()
        End Sub

        Private Sub UserType_Changed(sender As Object, e As SelectionChangedEventArgs)
            Dim item = TryCast(Me.UserType.SelectedItem, ComboBoxItem)
            If item IsNot Nothing Then Me.UserType.Tag = item.Content
        End Sub

        Private Sub LogIn_Click(sender As Object, e As RoutedEventArgs)
            If Me.UserType.SelectedIndex < 0 Then
                MessageBox.Show("Please choose Professor or Admin.", "Missing information", MessageBoxButton.OK, MessageBoxImage.Information)
                Return
            End If

            Dim employeeNumberMissing = String.IsNullOrWhiteSpace(Me.UserId.Text)
            Dim passwordMissing = String.IsNullOrEmpty(Me.Pwd.Password)

            If employeeNumberMissing AndAlso passwordMissing Then
                MessageBox.Show("Please enter your employee number and password.", "Missing information", MessageBoxButton.OK, MessageBoxImage.Information)
                Me.UserId.Focus()
                Return
            ElseIf employeeNumberMissing Then
                MessageBox.Show("Please enter your employee number.", "Missing information", MessageBoxButton.OK, MessageBoxImage.Information)
                Me.UserId.Focus()
                Return
            ElseIf passwordMissing Then
                MessageBox.Show("Please enter your password.", "Missing information", MessageBoxButton.OK, MessageBoxImage.Information)
                Me.Pwd.Focus()
                Return
            End If

            Dim employeeNumber = Me.UserId.Text.Trim()
            Dim expectedRole = If(Me.UserType.SelectedIndex = 0, "Professor", "Admin")
            Dim result = AuthManager.ValidateUserLogin(employeeNumber, Me.Pwd.Password, expectedRole)

            Select Case result
                Case AuthManager.LoginResult.Success
                    If Me.UserType.SelectedIndex = 0 Then
                        ProfileService.LoadProfile()
                    Else
                        Try
                            If Not Database.LoadAdminProfile(employeeNumber) Then
                                Session.SignOut()
                                MessageBox.Show("This employee account is not authorized as an administrator.", "Login failed", MessageBoxButton.OK, MessageBoxImage.Warning)
                                Return
                            End If
                        Catch ex As MySqlConnector.MySqlException
                            Session.SignOut()
                            MessageBox.Show("MariaDB connection failed." & Environment.NewLine & ex.Message, "Connection problem", MessageBoxButton.OK, MessageBoxImage.Error)
                            Return
                        End Try
                    End If

                    Dim selectedRole = If(Me.UserType.SelectedIndex = 0, Role.Prof, Role.Admin)
                    Dim window = New Window2(selectedRole)
                    window.Show()
                    Close()

                Case AuthManager.LoginResult.LockedOut
                    MessageBox.Show("Too many failed attempts. Please wait a minute and try again.", "Locked", MessageBoxButton.OK, MessageBoxImage.Warning)

                Case AuthManager.LoginResult.ServerError
                    MessageBox.Show("Can't reach the server right now. Please try again later.", "Connection problem", MessageBoxButton.OK, MessageBoxImage.Error)

                Case Else
                    MessageBox.Show("Invalid employee number or password.", "Login failed", MessageBoxButton.OK, MessageBoxImage.Warning)
            End Select
        End Sub
    End Class
End Namespace
