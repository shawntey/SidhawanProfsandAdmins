Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Controls.Primitives
Imports System.Windows.Data
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Threading

Namespace SidhawanForProfsAndAdmins
    Public Class Tbl
        Private ReadOnly _grid As DataGrid = New DataGrid With {
            .Style = SidhawanForProfsAndAdmins.Ui.S("GreenGrid")
        }
        Private ReadOnly _links As Dictionary(Of DataGridColumn, Action(Of Object)) = New Dictionary(Of DataGridColumn, Action(Of Object))()
        Private ReadOnly _editCols As List(Of DataGridColumn) = New List(Of DataGridColumn)()
        Private _headerDoubleClick As Action(Of String)
        Private _headerTripleClick As Action(Of String)
        Private _cellDoubleClick As Action(Of Object, String)
        Private _cellTripleClick As Action(Of Object, String)
        Private _headerDoubleTimer As DispatcherTimer
        Private _pendingHeader As String
        Private _boldRows As Boolean

        Private Sub New(rows As IEnumerable)
            _grid.ItemsSource = rows
        End Sub

        Public Shared Function [For](rows As IEnumerable) As Tbl
            Return New Tbl(rows)
        End Function

        Public Function Col(header As String, path As String, width As Double, Optional left As Boolean = False, Optional bold As Boolean = False) As Tbl
            Add(header, path, width, left, edit:=False, bold:=bold)
            Return Me
        End Function

        Public Function HeaderDoubleClick(action As Action(Of String)) As Tbl
            _headerDoubleClick = action
            Return Me
        End Function

        Public Function HeaderTripleClick(action As Action(Of String)) As Tbl
            _headerTripleClick = action
            Return Me
        End Function

        Public Function CellDoubleClick(action As Action(Of Object, String)) As Tbl
            _cellDoubleClick = action
            Return Me
        End Function

        Public Function CellTripleClick(action As Action(Of Object, String)) As Tbl
            _cellTripleClick = action
            Return Me
        End Function

        Public Function Link(header As String, path As String, width As Double, onClick As Action(Of Object), Optional left As Boolean = False) As Tbl
            _links(Add(header, path, width, left, edit:=False, link:=True)) = onClick
            Return Me
        End Function

        Public Function Edit(header As String, path As String, width As Double, Optional left As Boolean = False, Optional locked As Boolean = False) As Tbl
            Dim c = Add(header, path, width, left, edit:=True)
            c.IsReadOnly = locked
            _editCols.Add(c)
            Return Me
        End Function

        Public Function Cell(header As String, templateKey As String, width As Double, Optional onClick As Action(Of Object) = Nothing) As Tbl
            Dim c = New DataGridTemplateColumn With {
    .Header = header,
    .Width = Star(width),
    .IsReadOnly = True,
    .CellTemplate = SidhawanForProfsAndAdmins.Ui.R(Of DataTemplate)(templateKey)
}
            _grid.Columns.Add(c)
            If onClick IsNot Nothing Then _links(c) = onClick
            Return Me
        End Function

        Public Function Matrix(firstHeader As String, firstWidth As Double, headers As IEnumerable(Of String), widths As Double(), Optional edit As Boolean = False, Optional lockLast As Integer = 0, Optional choices As IEnumerable(Of String) = Nothing) As Tbl
            Dim list = headers.ToList()
            Add(firstHeader, "Label", firstWidth, left:=True, edit:=False)
            For i = 0 To list.Count - 1
                Dim editable = edit AndAlso i < list.Count - lockLast
                Dim path = "Cells[" & i.ToString() & "]"

                If editable AndAlso choices IsNot Nothing Then
                    AddChoice(list(i), path, widths(Math.Min(i, widths.Length - 1)), choices)
                Else
                    Add(list(i), path, widths(Math.Min(i, widths.Length - 1)), left:=False, edit:=editable)
                End If
            Next
            Return Me
        End Function

        Public Function Head(styleKey As String) As Tbl
            _grid.ColumnHeaderStyle = SidhawanForProfsAndAdmins.Ui.S(styleKey)
            Return Me
        End Function
        Public Function HeadH(height As Double) As Tbl
            _grid.ColumnHeaderHeight = height
            Return Me
        End Function
        Public Function RowH(height As Double) As Tbl
            _grid.MinRowHeight = height
            Return Me
        End Function
        Public Function NoHeader() As Tbl
            _grid.HeadersVisibility = DataGridHeadersVisibility.None
            Return Me
        End Function
        Public Function BoldRows() As Tbl
            _boldRows = True
            Return Me
        End Function

        Public Function Big() As Tbl
            _grid.FontFamily = New FontFamily("Verdana")
            _grid.FontSize = 16
            _grid.MinRowHeight = 47
            Return Me
        End Function

        Public Function Plain() As Tbl
            _grid.BorderThickness = New Thickness(0)
            _grid.CellStyle = SidhawanForProfsAndAdmins.Ui.S("GridCellLines")
            _grid.ColumnHeaderStyle = SidhawanForProfsAndAdmins.Ui.S("GridHeaderBold")
            Return Me
        End Function

        Public Function Build() As DataGrid
            If _boldRows Then
                For Each c In _grid.Columns.OfType(Of DataGridTextColumn)()
                    Dim style = New Style(GetType(TextBlock), c.ElementStyle)
                    Dim trigger = New DataTrigger()
                    trigger.Binding = New Binding("Bold")
                    trigger.Value = True
                    trigger.Setters.Add(New Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold))
                    style.Triggers.Add(trigger)
                    c.ElementStyle = style
                Next
            End If

            _grid.Tag = _editCols

            If _headerDoubleClick IsNot Nothing OrElse _headerTripleClick IsNot Nothing Then
                _headerDoubleTimer = New DispatcherTimer With {
                    .Interval = TimeSpan.FromMilliseconds(275)
                }
                AddHandler _headerDoubleTimer.Tick,
                    Sub(sender, args)
                        _headerDoubleTimer.Stop()
                        If _headerDoubleClick Is Nothing OrElse String.IsNullOrEmpty(_pendingHeader) Then Return

                        Dim header = _pendingHeader
                        _pendingHeader = Nothing
                        _headerDoubleClick(header)
                    End Sub

                AddHandler _grid.PreviewMouseLeftButtonDown,
                    Sub(sender, args)
                        Dim header = FindUp(Of DataGridColumnHeader)(TryCast(args.OriginalSource, DependencyObject))
                        If header Is Nothing OrElse header.Content Is Nothing Then Return

                        If args.ClickCount = 2 AndAlso _headerDoubleClick IsNot Nothing Then
                            _pendingHeader = header.Content.ToString()
                            _headerDoubleTimer.Stop()
                            _headerDoubleTimer.Start()
                            args.Handled = True
                        ElseIf args.ClickCount = 3 AndAlso _headerTripleClick IsNot Nothing Then
                            _headerDoubleTimer.Stop()
                            _pendingHeader = Nothing
                            _headerTripleClick(header.Content.ToString())
                            args.Handled = True
                        End If
                    End Sub
            End If

            AddHandler _grid.PreviewMouseLeftButtonUp,
                Sub(sender, args)
                    Dim clickedCell As DataGridCell = FindUp(Of DataGridCell)(TryCast(args.OriginalSource, DependencyObject))
                    If clickedCell Is Nothing Then Return
                    Dim go As Action(Of Object) = Nothing
                    If _links.TryGetValue(clickedCell.Column, go) AndAlso clickedCell.DataContext IsNot Nothing Then
                        go(clickedCell.DataContext)
                    End If
                End Sub

            If _cellDoubleClick IsNot Nothing OrElse _cellTripleClick IsNot Nothing Then
                AddHandler _grid.PreviewMouseLeftButtonDown,
                    Sub(sender, args)
                        Dim clickedCell = FindUp(Of DataGridCell)(TryCast(args.OriginalSource, DependencyObject))
                        If clickedCell Is Nothing OrElse clickedCell.Column Is Nothing OrElse clickedCell.DataContext Is Nothing Then Return

                        Dim column = If(clickedCell.Column.Header, "").ToString()
                        If args.ClickCount = 2 AndAlso _cellDoubleClick IsNot Nothing Then
                            _cellDoubleClick(clickedCell.DataContext, column)
                            args.Handled = True
                        ElseIf args.ClickCount = 3 AndAlso _cellTripleClick IsNot Nothing Then
                            Dim row = clickedCell.DataContext
                            _grid.Dispatcher.BeginInvoke(
                                New Action(
                                    Sub()
                                        Try
                                            _cellTripleClick(row, column)
                                        Catch ex As Exception
                                            System.Diagnostics.Trace.TraceError("Cell triple-click action failed: " & ex.ToString())
                                        End Try
                                    End Sub),
                                DispatcherPriority.Background)
                            args.Handled = True
                        End If
                    End Sub
            End If

            _grid.AddHandler(UIElement.GotFocusEvent, New RoutedEventHandler(Sub(s, e)
                                                                                 Dim c As DataGridCell = TryCast(e.OriginalSource, DataGridCell)
                                                                                 If c IsNot Nothing AndAlso Not c.IsReadOnly AndAlso Not c.IsEditing Then
                                                                                     _grid.BeginEdit(e)
                                                                                 End If
                                                                             End Sub))

            AddHandler _grid.PreviewMouseWheel, Sub(s, e)
                                                    Dim parent As UIElement = TryCast(_grid.Parent, UIElement)
                                                    If e.Handled OrElse parent Is Nothing Then Return
                                                    e.Handled = True
                                                    Dim args = New MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                                                    args.RoutedEvent = UIElement.MouseWheelEvent
                                                    args.Source = s
                                                    parent.RaiseEvent(args)
                                                End Sub

            AddHandler _grid.CellEditEnding, Sub(s, e)
                                                 If e.EditAction <> DataGridEditAction.Commit Then Return
                                                 Dim item = e.Row.Item
                                                 Dim column = ""
                                                 If e.Column.Header IsNot Nothing Then column = e.Column.Header.ToString()
                                                  _grid.Dispatcher.BeginInvoke(
                                                      New Action(
                                                          Sub()
                                                              SidhawanForProfsAndAdmins.Data.Edited(item, column)
                                                          End Sub),
                                                      DispatcherPriority.Background)
                                             End Sub

            Return _grid
        End Function

        Public Shared Sub ToggleEdit(g As DataGrid)
            SetLocked(g, Nothing)
        End Sub

        Public Shared Sub Lock(g As DataGrid)
            SetLocked(g, True)
        End Sub

        Private Shared Sub SetLocked(g As DataGrid, locked As Boolean?)
            g.CommitEdit(DataGridEditingUnit.Row, True)
            Dim cols As List(Of DataGridColumn) = TryCast(g.Tag, List(Of DataGridColumn))
            If cols IsNot Nothing Then
                For Each c In cols
                    c.IsReadOnly = If(locked.HasValue, locked.Value, Not c.IsReadOnly)
                Next
            End If
        End Sub

        Private Function Add(header As String, path As String, width As Double, left As Boolean, edit As Boolean, Optional link As Boolean = False, Optional bold As Boolean = False) As DataGridTextColumn
            Dim style = SidhawanForProfsAndAdmins.Ui.S(If(link, If(left, "GridLinkLeft", "GridLink"), If(left, "GridTextLeft", "GridText")))
            If bold Then
                style = New Style(GetType(TextBlock), style)
                style.Setters.Add(New Setter(TextBlock.FontWeightProperty, FontWeights.Bold))
            End If

            Dim binding = New Binding(path)
            binding.Mode = If(edit, BindingMode.TwoWay, BindingMode.OneWay)

            Dim col = New DataGridTextColumn With {
    .header = header,
    .binding = binding,
    .width = Star(width),
    .IsReadOnly = Not edit,
    .ElementStyle = style,
    .EditingElementStyle = SidhawanForProfsAndAdmins.Ui.S("GridEditBox")
}
            _grid.Columns.Add(col)
            Return col
        End Function

        Private Sub AddChoice(header As String, path As String, width As Double, choices As IEnumerable(Of String))
            Dim text = New FrameworkElementFactory(GetType(TextBlock))
            text.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center)
            text.SetBinding(TextBlock.TextProperty, New Binding(path))

            Dim combo = New FrameworkElementFactory(GetType(ComboBox))
            combo.SetValue(ComboBox.ItemsSourceProperty, choices.ToList())
            combo.SetValue(ComboBox.HorizontalContentAlignmentProperty, HorizontalAlignment.Center)
            combo.SetBinding(ComboBox.SelectedItemProperty, New Binding(path) With {
                .Mode = BindingMode.TwoWay,
                .UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            })

            Dim col = New DataGridTemplateColumn With {
                .Header = header,
                .Width = Star(width),
                .IsReadOnly = False,
                .CellTemplate = New DataTemplate With {.VisualTree = text},
                .CellEditingTemplate = New DataTemplate With {.VisualTree = combo}
            }

            _grid.Columns.Add(col)
            _editCols.Add(col)
        End Sub

        Private Shared Function Star(w As Double) As DataGridLength
            Return New DataGridLength(w, DataGridLengthUnitType.Star)
        End Function

        Private Shared Function FindUp(Of T As DependencyObject)(d As DependencyObject) As T
            While d IsNot Nothing AndAlso Not (TypeOf d Is T)
                If TypeOf d Is Visual Then
                    d = VisualTreeHelper.GetParent(d)
                Else
                    d = LogicalTreeHelper.GetParent(d)
                End If
            End While
            Return TryCast(d, T)
        End Function
    End Class
End Namespace
