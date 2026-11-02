Imports System
Imports System.Windows
Imports System.Windows.Controls

Namespace SidhawanForProfsAndAdmins
    Partial Public Module Pages
        Private ReadOnly RecordBands As String() = {"ATTENDANCE & CLASS PARTICIPATION (10%)", "WRITTEN WORK (20%)", "PERFORMANCE TASK (30%)", "MAJOR EXAM (40%)"}

        Private Function Opens(source As SidhawanForProfsAndAdmins.ListPage, nextPage As Func(Of Object, UserControl)) As Action(Of Object)
            Return Sub(row)
                       SidhawanForProfsAndAdmins.Data.Open(row)
                       source.GoTo(nextPage(row))
                   End Sub
        End Function

        Private Function RecordPage(title As String, Optional back As Func(Of UserControl) = Nothing, Optional configure As Action(Of SidhawanForProfsAndAdmins.ListPage) = Nothing) As UserControl
            Dim p = New SidhawanForProfsAndAdmins.ListPage(title)

            Dim kind = 0

            Dim classCombo = SidhawanForProfsAndAdmins.Ui.Combo("Class Record", 305, SidhawanForProfsAndAdmins.Data.RecordKinds)
            Dim category = SidhawanForProfsAndAdmins.Ui.Combo("Category", 204, SidhawanForProfsAndAdmins.Data.EntryCategories)
            Dim classItem = SidhawanForProfsAndAdmins.Ui.Combo("Class Item", 204, SidhawanForProfsAndAdmins.Data.ClassItems)
            Dim editItems = SidhawanForProfsAndAdmins.Ui.PillButton("Edit Items", 100, 50, 16, False)

            Dim show As Action = Sub()
                                     p.ShowArrows(kind > 0 OrElse back IsNot Nothing, kind < RecordBands.Length - 1)
                                     p.Band(RecordBands(kind))
                                     p.Table(EntryTable(kind))
                                 End Sub

            AddHandler classCombo.SelectionChanged, Sub(s, e)
                                                        kind = Math.Max(0, classCombo.SelectedIndex)
                                                        show()
                                                    End Sub

            p.Filter(classCombo, category, classItem)
            p.Tool(editItems)

            p.Back(Sub()
                       If kind > 0 Then
                           classCombo.SelectedIndex = kind - 1
                       ElseIf back IsNot Nothing Then
                           p.GoTo(back())
                       End If
                   End Sub)

            p.Forward(Sub()
                          If kind < RecordBands.Length - 1 Then
                              classCombo.SelectedIndex = kind + 1
                          End If
                      End Sub)

            If configure IsNot Nothing Then configure(p)

            show()
            Return p
        End Function

        Private Function EntryTable(kind As Integer) As UIElement
            Dim source As SidhawanForProfsAndAdmins.Sheet
            Dim first As String
            Dim firstWidth As Double
            Dim widths As Double()
            Dim lockLast As Integer

            If kind = 0 Then
                source = SidhawanForProfsAndAdmins.Data.AttendanceSheet
                first = "NAME"
                firstWidth = 235
                widths = New Double() {132}
                lockLast = 0
            ElseIf kind = 1 Then
                source = SidhawanForProfsAndAdmins.Data.WrittenSheet
                first = ""
                firstWidth = 553
                widths = New Double() {62, 62, 62, 95, 95, 100}
                lockLast = 3
            ElseIf kind = 2 Then
                source = SidhawanForProfsAndAdmins.Data.PerformanceSheet
                first = ""
                firstWidth = 553
                widths = New Double() {62, 62, 62, 95, 95, 100}
                lockLast = 3
            Else
                source = SidhawanForProfsAndAdmins.Data.ExamSheet
                first = ""
                firstWidth = 242
                widths = New Double() {141, 111, 101, 108, 97, 114, 112}
                lockLast = 5
            End If

            Dim headerHeight As Double = If(kind = 0, 65, 56)

            Return SidhawanForProfsAndAdmins.Tbl.For(CType(source.Rows, IEnumerable)).Head("GridHeaderBold").BoldRows().HeadH(headerHeight).RowH(46).Matrix(first, firstWidth, CType(source.Columns, IEnumerable(Of String)), widths, edit:=True, lockLast:=lockLast).Build()
        End Function

        Private Function SummaryTable() As UIElement
            Dim widths = New Double() {107, 96, 106, 103, 79, 69, 79, 52, 75}
            Return SidhawanForProfsAndAdmins.Tbl.For(CType(SidhawanForProfsAndAdmins.Data.SummarySheet.Rows, IEnumerable)).Head("GridHeaderBold").BoldRows().HeadH(54).RowH(45).Matrix("Student", 261, CType(SidhawanForProfsAndAdmins.Data.SummarySheet.Columns, IEnumerable(Of String)), widths).Build()
        End Function
    End Module
End Namespace
