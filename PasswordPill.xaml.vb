Imports System.Windows
Imports System.Windows.Controls

Namespace SidhawanForProfsAndAdmins

    Public Partial Class PasswordPill
        Inherits UserControl
        Public Shared ReadOnly HintProperty As DependencyProperty = DependencyProperty.Register(NameOf(PasswordPill.Hint), GetType(String), GetType(PasswordPill), New PropertyMetadata("", Sub(d, e) CType(d, PasswordPill).HintText.Text = CStr(e.NewValue)))

        Public Property Hint As String
            Get
                Return CStr(GetValue(HintProperty))
            End Get
            Set(value As String)
                SetValue(HintProperty, value)
            End Set
        End Property

        Public ReadOnly Property Password As String
            Get
                Return Me.Hidden.Password
            End Get
        End Property

        Private _syncing As Boolean

        Public Sub New()
            Me.InitializeComponent()
        End Sub

        Private Sub Hidden_PasswordChanged(sender As Object, e As RoutedEventArgs)
            If _syncing Then Return
            _syncing = True
            Me.Shown.Text = Me.Hidden.Password
            _syncing = False
            UpdateHint()
        End Sub

        Private Sub Shown_TextChanged(sender As Object, e As TextChangedEventArgs)
            If _syncing Then Return
            _syncing = True
            Me.Hidden.Password = Me.Shown.Text
            _syncing = False
            UpdateHint()
        End Sub

        Private Sub UpdateHint()
            Me.HintText.Visibility = If(Me.Hidden.Password.Length = 0, Visibility.Visible, Visibility.Collapsed)
        End Sub

        Private Sub Eye_Click(sender As Object, e As RoutedEventArgs)
            Dim show As Boolean = Me.Eye.IsChecked = True
            Me.Shown.Visibility = If(show, Visibility.Visible, Visibility.Collapsed)
            Me.Hidden.Visibility = If(show, Visibility.Collapsed, Visibility.Visible)
            If show Then
                Me.Shown.Focus()
            Else
                Me.Hidden.Focus()
            End If
        End Sub

    End Class
End Namespace
