Imports System
Imports System.Windows
Imports System.Windows.Controls

Namespace SidhawanForProfsAndAdmins


    Public Partial Class ListPage
        Inherits UserControl
        Private _back, _forward, _edit, _confirm As Action
        Private _editWasCancelled As Boolean
        Private _confirmResult As Func(Of Boolean)
        Private _confirmNotice As String = "Changes saved successfully."

        Public ReadOnly Property Popup As SidhawanForProfsAndAdmins.MessageDialog
            Get
                Return Me.Dialog
            End Get
        End Property

        Public Sub New(title As String)
            Me.InitializeComponent()
            Me.TitleText.Text = title
        End Sub
        Public Function BackTo(page As Func(Of UserControl)) As ListPage
            Return Back(Sub() [GoTo](page()))
        End Function
        Public Function Back(onClick As Action) As ListPage
            _back = onClick
            Me.BackBtn.Visibility = Visibility.Visible
            Return Me
        End Function

        Public Function ConfirmNotice(message As String) As ListPage
            _confirmNotice = If(message, "")
            Return Me
        End Function

        Public Function ConfirmResult(onClick As Func(Of Boolean), Optional show As Boolean = True) As ListPage
            _confirmResult = onClick
            If show Then
                Me.ConfirmBtn.Visibility = Visibility.Visible
            End If
            Return Me
        End Function
        Public Function Forward(onClick As Action) As ListPage
            _forward = onClick
            Me.NextBtn.Visibility = Visibility.Visible
            Return Me
        End Function
        Public Function Pencil(Optional onClick As Action = Nothing) As ListPage
            _edit = onClick
            Me.EditBtn.Visibility = Visibility.Visible
            Return Me
        End Function

        Public Sub CancelEdit()
            _editWasCancelled = True
            Me.EditBtn.Visibility = Visibility.Visible
            Me.ConfirmBtn.Visibility = Visibility.Collapsed
        End Sub
        Public Function Confirm(Optional onClick As Action = Nothing, Optional show As Boolean = True) As ListPage
            _confirm = onClick
            If show Then
                Me.ConfirmBtn.Visibility = Visibility.Visible
            End If
            Return Me
        End Function
        Public Sub ShowArrows(back As Boolean, forward As Boolean)
            Me.BackBtn.Visibility = If(back, Visibility.Visible, Visibility.Collapsed)
            Me.NextBtn.Visibility = If(forward, Visibility.Visible, Visibility.Collapsed)
        End Sub

        Public Function Filter(ParamArray combos As ComboBox()) As ListPage
            For Each c In combos
                Me.FilterBar.Children.Add(c)
            Next

            Return Me
        End Function
        Public Function Tool(pTool As UIElement) As ListPage
            Me.ToolHost.Content = pTool
            Return Me
        End Function
        Private _light As Boolean

        Public Function Light() As ListPage
            _light = True
            Return Me
        End Function

        Public Function Band(text As String) As ListPage
            Me.BandHost.Content = SidhawanForProfsAndAdmins.Ui.Band(text, _light)
            Return Me
        End Function

        Public Function Band(text As Func(Of String), ParamArray watch As ComboBox()) As ListPage
            Dim lBand = SidhawanForProfsAndAdmins.Ui.Band(text(), _light)
            Dim label = CType(lBand.Child, TextBlock)
            For Each c In watch
                AddHandler c.SelectionChanged, Sub(src, args) label.Text = text()
            Next
            Me.BandHost.Content = lBand
            Return Me
        End Function
        Public Function Table(content As UIElement) As ListPage
            Me.BodyHost.Content = content
            Return Me
        End Function

        Public Function Plain() As ListPage
            Me.Box.BorderThickness = New Thickness(0)
            Return Me
        End Function
        Public Function SmallTitle() As ListPage
            Me.TitleText.FontSize = 21
            Return Me
        End Function
        Public Function Gap(top As Double) As ListPage
            Me.Box.Margin = New Thickness(0, top, 0, 0)
            Return Me
        End Function

        Public Function Wide(Optional filterIndent As Double = 0) As ListPage
            Me.Box.Margin = New Thickness(-13, Me.Box.Margin.Top, 30, 0)
            Me.FilterBar.Margin = New Thickness(filterIndent, 0, 0, 0)
            Me.ToolStrip.Margin = New Thickness(0, -12, -25, 0)
            Return Me
        End Function

        Public Sub [GoTo](page As UserControl)
            If Data.HasUnsavedEdits AndAlso Data.DiscardPendingEdits IsNot Nothing Then
                Data.DiscardPendingEdits.Invoke()
            End If
            TryCast(Window.GetWindow(Me), SidhawanForProfsAndAdmins.Window2)?.Navigate(page)
        End Sub

        Private Sub Back_Click(sender As Object, e As RoutedEventArgs)
            If Data.HasUnsavedEdits AndAlso Data.DiscardPendingEdits IsNot Nothing Then
                Data.DiscardPendingEdits.Invoke()
            End If
            _back?.Invoke()
        End Sub
        Private Sub Next_Click(sender As Object, e As RoutedEventArgs)
            If Data.HasUnsavedEdits AndAlso Data.DiscardPendingEdits IsNot Nothing Then
                Data.DiscardPendingEdits.Invoke()
            End If
            _forward?.Invoke()
        End Sub
        Private Sub Edit_Click(sender As Object, e As RoutedEventArgs)
            _editWasCancelled = False

            If _edit IsNot Nothing Then
                _edit()
            Else
                SidhawanForProfsAndAdmins.Ui.ToggleEdit(Me)
            End If

            If _editWasCancelled Then Return

            Me.EditBtn.Visibility = Visibility.Collapsed
            Me.ConfirmBtn.Visibility = Visibility.Visible
        End Sub

        Private Sub Confirm_Click(sender As Object, e As RoutedEventArgs)
            If _confirmResult IsNot Nothing Then
                If Not _confirmResult() Then Return
            Else
                If _confirm IsNot Nothing Then
                    _confirm()
                Else

                End If
            End If
            Me.ConfirmBtn.Visibility = Visibility.Collapsed
            Me.EditBtn.Visibility = Visibility.Visible

            Dim notice = _confirmNotice
            _confirmNotice = "Changes saved successfully."
            If Not String.IsNullOrWhiteSpace(notice) Then
                Popup.ShowNotice("check", notice)
            End If
        End Sub
        Private Sub Export_Click(sender As Object, e As RoutedEventArgs)

        End Sub

    End Class
End Namespace
