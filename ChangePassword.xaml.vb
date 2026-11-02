Imports System.Windows
Imports System.Windows.Controls
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Windows.Media

Namespace SidhawanForProfsAndAdmins

    Partial Public Class ChangePassword
        Inherits UserControl
        Public Sub New()
            Me.InitializeComponent()
            NewPw.AddHandler(PasswordBox.PasswordChangedEvent, New RoutedEventHandler(AddressOf OnNewPwTyped), True)
            NewPw.AddHandler(TextBox.TextChangedEvent, New TextChangedEventHandler(AddressOf OnNewPwTyped), True)
        End Sub

        Private Sub OnNewPwTyped(sender As Object, e As RoutedEventArgs)
            Dim password = ReadText(NewPw)
            RuleLength.IsChecked = Passwords.HasLength(password)
            RuleUpper.IsChecked = Passwords.HasUpper(password)
            RuleLower.IsChecked = Passwords.HasLower(password)
            RuleNumber.IsChecked = Passwords.HasDigit(password)
            RuleSpecial.IsChecked = Passwords.HasSpecial(password)
        End Sub

        Private Sub Change_Click(sender As Object, e As RoutedEventArgs)
            Dim currentPassword = ReadText(CurrentPw)
            Dim newPassword = ReadText(NewPw)
            Dim confirmPassword = ReadText(ConfirmPw)

            If String.IsNullOrEmpty(currentPassword) Then
                Me.Dialog.ShowNotice("incorrect", "Enter your current password.")
                Return
            End If
            If Not Passwords.MeetsPolicy(newPassword) Then
                Me.Dialog.ShowNotice("incorrect", "The new password does not meet all requirements.")
                Return
            End If

            If Not String.Equals(newPassword, confirmPassword, StringComparison.Ordinal) Then
                Me.Dialog.ShowNotice("incorrect", "The new passwords do not match.")
                Return
            End If

            Me.Dialog.ShowConfirm("Are you sure you want to change your password?", "", "", AddressOf ChangePassword)
        End Sub

        Private Sub ChangePassword()
            Select Case AuthManager.ChangePassword(CurrentPw.Password, NewPw.Password)
                Case AuthManager.ChangeResult.Success
                    Me.Dialog.ShowNotice("check", "Password changed successfully!")
                    ClearPill(CurrentPw)
                    ClearPill(NewPw)
                    ClearPill(ConfirmPw)
                Case AuthManager.ChangeResult.NotLoggedIn
                    Me.Dialog.ShowNotice("incorrect", "You must be logged in to change your password.")
                Case AuthManager.ChangeResult.WrongCurrent
                    Me.Dialog.ShowNotice("incorrect", "The current password is incorrect.")
                Case AuthManager.ChangeResult.SameAsOld
                    Me.Dialog.ShowNotice("incorrect", "The new password must differ from the current password.")
                Case AuthManager.ChangeResult.WeakPassword
                    Me.Dialog.ShowNotice("incorrect", "The new password does not meet all requirements.")
                Case Else
                    Me.Dialog.ShowNotice("incorrect", "Could not change your password. Please try again later.")
            End Select
        End Sub

        Private Shared Function ReadText(pill As DependencyObject) As String
            Dim passwordBox = FindAll(Of PasswordBox)(pill).FirstOrDefault()
            If passwordBox IsNot Nothing AndAlso passwordBox.Password <> "" Then Return passwordBox.Password

            Dim textBox = FindAll(Of TextBox)(pill).FirstOrDefault()
            Return If(textBox IsNot Nothing, textBox.Text, "")
        End Function

        Private Shared Sub ClearPill(pill As DependencyObject)
            For Each passwordBox In FindAll(Of PasswordBox)(pill)
                passwordBox.Password = ""
            Next
            For Each textBox In FindAll(Of TextBox)(pill)
                textBox.Text = ""
            Next
        End Sub

        Private Shared Function FindAll(Of T As DependencyObject)(root As DependencyObject) As List(Of T)
            Dim result As New List(Of T)()
            CollectAll(root, result)
            Return result
        End Function

        Private Shared Sub CollectAll(Of T As DependencyObject)(root As DependencyObject, result As List(Of T))
            For i = 0 To VisualTreeHelper.GetChildrenCount(root) - 1
                Dim child = VisualTreeHelper.GetChild(root, i)
                Dim match = TryCast(child, T)
                If match IsNot Nothing Then result.Add(match)
                CollectAll(child, result)
            Next
        End Sub
    End Class
End Namespace
