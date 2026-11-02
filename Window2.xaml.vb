Imports System
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports System.Windows.Media.Imaging

Namespace SidhawanForProfsAndAdmins
    Public Enum Role
        Prof
        Admin
    End Enum

    Partial Public Class Window2
        Inherits Window
        Private ReadOnly _role As Role
        Private _lastNavIndex As Integer = -1
        Private _changingNavigation As Boolean

        Public Sub New(userRole As Role)
            Me.InitializeComponent()
            _role = userRole
            Me.WelcomeLastName.Text = If(String.IsNullOrWhiteSpace(Session.UserName), "", Session.UserName & "!")

            Dim sideKey As String
            Dim pages As String()

            If userRole = Role.Prof Then
                sideKey = "ProfSide"
                pages = New String() {"Professor Information", "Students", "Grades", "Summary of Record", "Change Password"}
            Else
                sideKey = "AdminSide"
                pages = New String() {"Admin Information", "Students", "Summary of Record", "Professors", "History", "Grading Rules & Policy", "Change Password"}
            End If

            Me.Sidebar.Background = CType(FindResource(sideKey), Brush)

            Dim brush = New ImageBrush(New BitmapImage(New Uri("pack://application:,,,/Images/Pfp.png")))
            brush.Stretch = Stretch.UniformToFill
            Me.SideAvatar.Fill = brush
            Me.HeaderAvatar.Fill = brush

            Me.NavList.ItemsSource = pages

            AddHandler Me.NavList.SelectionChanged,
                Sub(s, e)
                    If _changingNavigation Then Return

                    Dim requestedIndex = Me.NavList.SelectedIndex

                    If Data.HasUnsavedEdits AndAlso Data.DiscardPendingEdits IsNot Nothing Then
                        Data.DiscardPendingEdits.Invoke()
                    End If

                    _lastNavIndex = requestedIndex
                    ShowPage()
                End Sub
            Me.NavList.SelectedIndex = 0
        End Sub

        Public Sub Navigate(view As UserControl)
            Me.Body.Content = view
        End Sub

        Private Shared Function FindAncestor(Of T As DependencyObject)(source As DependencyObject) As T
            Dim current = source

            While current IsNot Nothing
                Dim match = TryCast(current, T)
                If match IsNot Nothing Then Return match
                current = VisualTreeHelper.GetParent(current)
            End While

            Return Nothing
        End Function

        Private Sub ShowPage()
            Dim page As String = TryCast(Me.NavList.SelectedItem, String)
            If Equals(page, Nothing) Then Return
            SidhawanForProfsAndAdmins.Data.Request(_role, page)

            Dim view As UserControl = Nothing

            If Equals(page, "Change Password") OrElse Equals(page, "Password") Then
                view = New SidhawanForProfsAndAdmins.ChangePassword()
            ElseIf _role = Role.Prof Then
                If Equals(page, "Professor Information") Then
                    view = SidhawanForProfsAndAdmins.Pages.ProfInfo()
                ElseIf Equals(page, "Students") Then
                    view = SidhawanForProfsAndAdmins.Pages.ProfStudents()
                ElseIf Equals(page, "Grades") Then
                    view = SidhawanForProfsAndAdmins.Pages.ProfGrades()
                ElseIf Equals(page, "Summary of Record") Then
                    view = SidhawanForProfsAndAdmins.Pages.ProfSummary()
                End If
            Else
                If Equals(page, "Admin Information") Then
                    view = SidhawanForProfsAndAdmins.Pages.AdminInfo()
                ElseIf Equals(page, "Students") Then
                    view = SidhawanForProfsAndAdmins.Pages.AdminStudents()
                ElseIf Equals(page, "Summary of Record") Then
                    view = SidhawanForProfsAndAdmins.Pages.AdminSummaryDepartments()
                ElseIf Equals(page, "Professors") Then
                    view = SidhawanForProfsAndAdmins.Pages.AdminProfessorDepartments()
                ElseIf Equals(page, "History") Then
                    view = SidhawanForProfsAndAdmins.Pages.History()
                ElseIf Equals(page, "Grading Rules & Policy") Then
                    view = SidhawanForProfsAndAdmins.Pages.GradingPolicy()
                End If
            End If

            Me.Body.Content = view
        End Sub

        Private Sub LogOut_Click(sender As Object, e As RoutedEventArgs)
            Dim login As Window = New SidhawanForProfsAndAdmins.LogInWindow()
            login.Show()
            Close()
        End Sub

        Private Sub TOS_Click(sender As Object, e As RoutedEventArgs)
            Me.Dialog.ShowText("Terms of Service",
                "1. Use your own account only." & vbCrLf & vbCrLf &
                "2. Do not share your password." & vbCrLf & vbCrLf &
                "3. Your records are for your own viewing." & vbCrLf & vbCrLf)
        End Sub

        Private Sub PrivPolicy_Click(sender As Object, e As RoutedEventArgs)
            Me.Dialog.ShowText("Privacy Policy",
                "1. Use your own account only." & vbCrLf & vbCrLf &
                "2. Do not share your password." & vbCrLf & vbCrLf &
                "3. Your records are for your own viewing." & vbCrLf & vbCrLf)
        End Sub

        Private Sub Guidelines_Click(sender As Object, e As RoutedEventArgs)
            Me.Dialog.ShowText("Guidelines",
                "1. Use your own account only." & vbCrLf & vbCrLf &
                "2. Do not share your password." & vbCrLf & vbCrLf &
                "3. Your records are for your own viewing." & vbCrLf & vbCrLf)
        End Sub
    End Class
End Namespace
