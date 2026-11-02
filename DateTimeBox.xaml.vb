Imports System
Imports System.Globalization
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Controls.Primitives
Imports System.Windows.Input

Namespace SidhawanForProfsAndAdmins
    Public Partial Class DateTimeBox
        Inherits UserControl
        Public Property Label As String = ""

        Public Property EndOfDay As Boolean

        Private _value As Date?

        Public Property Value As Date?
            Get
                Return _value
            End Get
            Set(value As Date?)
                _value = value
                Me.ValueText.Text = FormatDate(value)
            End Set
        End Property

        Public Event ValueChanged As Action(Of Date?)

        Public Sub New()
            Me.InitializeComponent()

            AddHandler Me.Cal.PreviewMouseUp, Sub(s, e)
                                                  If TypeOf Mouse.Captured Is CalendarItem Then Mouse.Capture(Nothing)
                                              End Sub
        End Sub

        Private Shared Function FormatDate(d As Date?) As String
            If d.HasValue Then Return d.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            Return ""
        End Function

        Private Sub Box_Click(sender As Object, e As MouseButtonEventArgs)
            If _value.HasValue Then
                Me.Cal.SelectedDate = _value.Value.Date
                Me.Cal.DisplayDate = _value.Value
            Else
                Me.Cal.SelectedDate = Nothing
                Me.Cal.DisplayDate = Date.Today
            End If
            Me.Pop.IsOpen = True
        End Sub

        Private Sub Ok_Click(sender As Object, e As RoutedEventArgs)
            Dim selected As Date? = Me.Cal.SelectedDate
            If Not selected.HasValue Then
                Me.Pop.IsOpen = False
                Return
            End If
            ApplyValue(selected)
        End Sub

        Private Sub Clear_Click(sender As Object, e As RoutedEventArgs)
            ApplyValue(Nothing)
        End Sub

        Private Sub ApplyValue(newValue As Date?)
            Value = newValue
            Me.Pop.IsOpen = False
            RaiseEvent ValueChanged(newValue)
            SidhawanForProfsAndAdmins.Data.RaiseFilterChanged(Label, FormatDate(newValue))
        End Sub

        Private Sub DigitsOnly(sender As Object, e As TextCompositionEventArgs)
            For Each ch In e.Text
                If Not Char.IsDigit(ch) Then
                    e.Handled = True
                    Return
                End If
            Next
            e.Handled = False
        End Sub
    End Class
End Namespace
