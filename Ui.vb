Imports System.Collections
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media

Namespace SidhawanForProfsAndAdmins
    Public Module Ui
        Public Function S(key As String) As Style
            Return CType(Application.Current.FindResource(key), Style)
        End Function

        Public Function R(Of T)(key As String) As T
            Return Application.Current.FindResource(key)
        End Function

        Public Function B(key As String) As Brush
            Return CType(Application.Current.FindResource(key), Brush)
        End Function

        Public Function Combo(label As String, width As Double, items As IEnumerable) As ComboBox
            Return MakeCombo("PillCombo", label, width, items, New Thickness(0, 0, 9, 0))
        End Function

        Private Function MakeCombo(style As String, label As String, width As Double, items As IEnumerable, margin As Thickness) As ComboBox
            Dim combo = New ComboBox With {
    .Style = S(style),
    .Tag = label,
    .Width = width,
    .Margin = margin,
    .FontSize = 16,
    .ItemsSource = items,
                .IsSynchronizedWithCurrentItem = False,
                .SelectedIndex = -1
}
            AddHandler combo.SelectionChanged, Sub(s, e)
                                                   Dim value As String = TryCast(combo.SelectedItem, String)
                                                   If Not Equals(value, Nothing) Then
                                                       SidhawanForProfsAndAdmins.Data.RaiseFilterChanged(label, value)
                                                   End If
                                               End Sub
            Return combo
        End Function

        Public Function Sel(c As ComboBox) As String
            Dim value As String = TryCast(c.SelectedItem, String)
            If Equals(value, Nothing) Then Return ""
            Return value
        End Function

        Public Function Code(course As String) As String
            Return course.Split("|"c)(0).Trim()
        End Function

        Public Function Pretty(upper As String) As String
            Return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(upper.ToLower())
        End Function

        Public Function Band(text As String, Optional light As Boolean = False) As Border
            Dim textBlock = New TextBlock()
            textBlock.Text = text
            textBlock.Style = S("BandText")

            Dim border = New Border()
            border.Height = 53
            border.Margin = New Thickness(0, 4, 0, 0)
            border.Background = B(If(light, "NavActive", "Brand"))
            border.Child = textBlock
            Return border
        End Function

        Public Function PillButton(text As String, width As Double, height As Double, Optional fontSize As Double = 17, Optional underline As Boolean = False) As Button
            Dim label = New TextBlock With {
                .Text = text
            }
            If underline Then label.TextDecorations = TextDecorations.Underline
            Return New Button With {
                .Style = S("PillButton"),
                .Content = label,
                .Width = width,
                .Height = height,
                .FontSize = fontSize
            }
        End Function

        Public Function IconBtn(file As String, Optional size As Double = 30) As Button
            Dim icon = New SidhawanForProfsAndAdmins.Icon()
            icon.File = file
            icon.Width = size
            icon.Height = size

            Dim button = New Button()
            button.Style = S("IconButton")
            button.Content = icon
            Return button
        End Function

        Public Function Stack(ParamArray items As UIElement()) As StackPanel
            Dim panel = New StackPanel()
            For Each item In items
                panel.Children.Add(item)
            Next
            Return panel
        End Function

        Public Sub ToggleEdit(root As DependencyObject)
            For Each f In Find(Of SidhawanForProfsAndAdmins.InfoField)(root)
                f.IsEditable = Not f.IsEditable
            Next
        End Sub

        Private Iterator Function Find(Of T As DependencyObject)(root As DependencyObject) As IEnumerable(Of T)
            For Each child In LogicalTreeHelper.GetChildren(root).OfType(Of DependencyObject)()
                Dim match As T = TryCast(child, T)
                If match IsNot Nothing Then Yield match
                For Each d In Find(Of T)(child)
                    Yield d
                Next
            Next
        End Function
    End Module
End Namespace
