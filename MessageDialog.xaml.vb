Imports System
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Documents
Imports System.Windows.Media


Namespace SidhawanForProfsAndAdmins

    Public Partial Class MessageDialog
        Inherits UserControl
        Private _onYes As Action

        Public Sub New()
            Me.InitializeComponent()
        End Sub
        Public Sub ShowConfirm(before As String, bold As String, after As String, onYes As Action)
            TermsPanel.Visibility = Visibility.Collapsed
            _onYes = onYes
            Me.ConfirmText.Inlines.Clear()
            Me.ConfirmText.Inlines.Add(New Run(before))
            Me.ConfirmText.Inlines.Add(New Run(bold) With {
        .FontWeight = FontWeights.Bold,
        .Foreground = CType(FindResource("Brand"), Brush)
    })
            Me.ConfirmText.Inlines.Add(New Run(after))

            Me.ConfirmPanel.Visibility = Visibility.Visible
            Me.NoticePanel.Visibility = Visibility.Collapsed
            Visibility = Visibility.Visible
        End Sub

        ''' <paramname="kind">"check", "lock" or "unlock"</param>
        ''' <paramname="kind">"check", "lock" or "unlock"</param>
        Public Sub ShowNotice(kind As String, message As String)
            TermsPanel.Visibility = Visibility.Collapsed
            Me.NoticeText.Text = message
            BuildIcon(kind)

            Me.NoticePanel.Visibility = Visibility.Visible
            Me.ConfirmPanel.Visibility = Visibility.Collapsed
            Visibility = Visibility.Visible
        End Sub

        Public Sub ShowText(title As String, body As String)
            NoticePanel.Visibility = Visibility.Collapsed
            ConfirmPanel.Visibility = Visibility.Collapsed
            TermsPanel.Visibility = Visibility.Visible
            TermsTitle.Text = title
            TermsText.Text = body
            Visibility = Visibility.Visible
        End Sub

        Private Sub BuildIcon(kind As String)
            Me.IconHost.Children.Clear()
            Me.IconHost.Children.Add(New SidhawanForProfsAndAdmins.Icon With {
                    .File = If(String.Equals(kind, "cross", StringComparison.OrdinalIgnoreCase), "incorrect", kind)
                })
        End Sub

        Private Sub Close_Click(sender As Object, e As RoutedEventArgs)
            Visibility = Visibility.Collapsed
        End Sub

        Private Sub Yes_Click(sender As Object, e As RoutedEventArgs)
            Visibility = Visibility.Collapsed
            Dim action = _onYes
            _onYes = Nothing
            action?.Invoke()
        End Sub

    End Class
End Namespace
