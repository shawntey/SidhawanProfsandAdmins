Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Windows
Imports Microsoft.Win32
Imports PdfSharpCore
Imports PdfSharpCore.Drawing
Imports PdfSharpCore.Pdf

Namespace SidhawanForProfsAndAdmins
    Public Class PdfExportCategory
        Public Property Name As String = ""
        Public Property Sheet As Sheet

        Public Sub New(name As String, sheet As Sheet)
            Me.Name = name
            Me.Sheet = sheet
        End Sub
    End Class

    Public NotInheritable Class PdfExportService
        Private Sub New()
        End Sub

        Public Shared Function ExportSummary(title As String,
                                              schoolYear As String,
                                              section As String,
                                              course As String,
                                              sheet As Sheet) As Boolean
            Return ExportCategories(
                title,
                New String() {
                    "School Year: " & DisplayFilter(schoolYear),
                    "Section: " & DisplayFilter(section),
                    "Course: " & DisplayFilter(course)},
                New PdfExportCategory() {
                    New PdfExportCategory("Summary of Record", sheet)})
        End Function

        Public Shared Function ExportProfessorGrades(title As String,
                                                     term As String,
                                                     component As String,
                                                     itemName As String,
                                                     rows As IEnumerable(Of ProfessorItemizedGradeRow)) As Boolean
            Dim sheet As New Sheet()
            sheet.Columns.Add("Item")
            sheet.Columns.Add("Highest Score")
            sheet.Columns.Add("Acquired Score")
            sheet.Columns.Add("Weight")

            If rows IsNot Nothing Then
                For Each row In rows
                    If row Is Nothing Then Continue For

                    sheet.Rows.Add(
                        MatrixRow.Create(
                            row.StudentName,
                            row.ItemName,
                            row.HighestScore,
                            row.AcquiredScore,
                            row.ComponentWeight))
                Next
            End If

            Return ExportCategories(
                title,
                New String() {
                    "Class Record: " & DisplayFilter(term),
                    "Category: " & DisplayFilter(component),
                    "Class Item: " & DisplayFilter(itemName)},
                New PdfExportCategory() {
                    New PdfExportCategory("Professor Grades", sheet)})
        End Function

        Public Shared Function ExportCategories(title As String,
                                                 filters As IEnumerable(Of String),
                                                 categories As IEnumerable(Of PdfExportCategory)) As Boolean
            Dim dialog As New SaveFileDialog With {
                .Title = "Export " & title,
                .Filter = "PDF files (*.pdf)|*.pdf",
                .DefaultExt = ".pdf",
                .FileName = SafeFileName(title) & ".pdf",
                .AddExtension = True
            }

            If dialog.ShowDialog() <> True Then Return False

            Try
                Using document As New PdfDocument()
                    Dim page = document.AddPage()
                    page.Size = PageSize.A4
                    page.Orientation = PageOrientation.Landscape
                    Dim graphics = XGraphics.FromPdfPage(page)
                    Dim regular = New XFont("Arial", 8, XFontStyle.Regular)
                    Dim bold = New XFont("Arial", 8, XFontStyle.Bold)
                    Dim titleFont = New XFont("Arial", 16, XFontStyle.Bold)
                    Dim categoryFont = New XFont("Arial", 10, XFontStyle.Bold)
                    Dim green = New XSolidBrush(XColor.FromArgb(50, 126, 52))
                    Dim lightGreen = New XSolidBrush(XColor.FromArgb(221, 253, 224))
                    Dim linePen = New XPen(XColors.Gray, 0.5)
                    Dim margin As Double = 30
                    Dim tableWidth = page.Width.Point - (margin * 2)
                    Dim labelWidth As Double = 190
                    Dim valueWidth As Double = 0
                    Dim y As Double = 28
                    Dim firstPage As Boolean = True

                    For Each category In categories
                        If category Is Nothing OrElse category.Sheet Is Nothing Then Continue For
                        If category.Sheet.Columns.Count = 0 Then Continue For

                        valueWidth = (tableWidth - labelWidth) / category.Sheet.Columns.Count
                        If Not firstPage Then
                            graphics.Dispose()
                            page = document.AddPage()
                            page.Size = PageSize.A4
                            page.Orientation = PageOrientation.Landscape
                            graphics = XGraphics.FromPdfPage(page)
                            y = margin
                        End If
                        firstPage = False

                        If y = 28 Then
                            graphics.DrawString(title, titleFont, green, New XPoint(margin, y))
                            y += 22
                            graphics.DrawString(String.Join("    ", filters), regular, XBrushes.Black, New XPoint(margin, y))
                            y += 18
                        End If

                        graphics.DrawString(category.Name, categoryFont, green, New XPoint(margin, y))
                        y += 16
                        Dim headerSize = HeaderHeight(category.Sheet, bold, labelWidth, valueWidth)
                        If y + headerSize > page.Height.Point - margin Then
                            graphics.Dispose()
                            page = document.AddPage()
                            page.Size = PageSize.A4
                            page.Orientation = PageOrientation.Landscape
                            graphics = XGraphics.FromPdfPage(page)
                            y = margin
                        End If
                        DrawHeaderRow(graphics, category.Sheet, margin, y, labelWidth, valueWidth, bold, lightGreen, linePen)
                        y += headerSize

                        For Each row In category.Sheet.Rows
                            Dim rowHeight = ContentHeight(row, regular, labelWidth, valueWidth)
                            If y + rowHeight > page.Height.Point - margin Then
                                graphics.Dispose()
                                page = document.AddPage()
                                page.Size = PageSize.A4
                                page.Orientation = PageOrientation.Landscape
                                graphics = XGraphics.FromPdfPage(page)
                                y = margin
                                DrawHeaderRow(graphics, category.Sheet, margin, y, labelWidth, valueWidth, bold, lightGreen, linePen)
                                y += headerSize
                            End If

                            DrawRow(graphics, row, margin, y, labelWidth, valueWidth, rowHeight, regular, lightGreen, linePen)
                            y += rowHeight
                        Next
                        y += 12
                    Next

                    graphics.Dispose()
                    document.Save(dialog.FileName)
                End Using

                MessageBox.Show("Exported successfully." & Environment.NewLine & dialog.FileName,
                                "Export complete", MessageBoxButton.OK, MessageBoxImage.Information)
                Return True
            Catch ex As Exception
                System.Diagnostics.Trace.TraceError("PDF export failed: " & ex.ToString())
                MessageBox.Show("Could not export the table to PDF. Please try again later.",
                                "Export failed", MessageBoxButton.OK, MessageBoxImage.Error)
                Return False
            End Try
        End Function

        Private Shared Sub DrawHeaderRow(graphics As XGraphics, sheet As Sheet, margin As Double, y As Double,
                                         labelWidth As Double, valueWidth As Double, font As XFont,
                                         fill As XBrush, pen As XPen)
            Dim height = HeaderHeight(sheet, font, labelWidth, valueWidth)
            graphics.DrawRectangle(fill, margin, y, labelWidth + valueWidth * sheet.Columns.Count, height)
            graphics.DrawRectangle(pen, margin, y, labelWidth + valueWidth * sheet.Columns.Count, height)
            DrawWrapped(graphics, "Student", font, XBrushes.Black, New XRect(margin + 3, y + 2, labelWidth - 6, height), True, True)
            For i = 0 To sheet.Columns.Count - 1
                Dim x = margin + labelWidth + i * valueWidth
                graphics.DrawRectangle(pen, x, y, valueWidth, height)
                DrawWrapped(graphics, sheet.Columns(i), font, XBrushes.Black, New XRect(x + 2, y + 2, valueWidth - 4, height), True, True)
            Next
        End Sub

        Private Shared Sub DrawRow(graphics As XGraphics, row As MatrixRow, margin As Double, y As Double,
                                   labelWidth As Double, valueWidth As Double, height As Double,
                                   font As XFont, fill As XBrush, pen As XPen)
            Dim tableWidth = labelWidth + valueWidth * row.Cells.Count
            Dim rowFill = If(row.Bold, fill, XBrushes.White)
            graphics.DrawRectangle(rowFill, margin, y, tableWidth, height)
            graphics.DrawRectangle(pen, margin, y, tableWidth, height)
            DrawWrapped(graphics, row.Label, font, XBrushes.Black, New XRect(margin + 4, y + 2, labelWidth - 8, height), False)
            For i = 0 To row.Cells.Count - 1
                Dim x = margin + labelWidth + i * valueWidth
                graphics.DrawRectangle(pen, x, y, valueWidth, height)
                DrawWrapped(graphics, row.Cells(i), font, XBrushes.Black, New XRect(x + 3, y + 2, valueWidth - 6, height), True, False)
            Next
        End Sub

        Private Shared Function HeaderHeight(sheet As Sheet, font As XFont, labelWidth As Double, valueWidth As Double) As Double
            Dim lines = WrapText("Student", font, labelWidth - 6).Count
            For Each column In sheet.Columns
                lines = Math.Max(lines, HeaderLineCount(column, font, valueWidth - 4))
            Next
            Return Math.Max(46, lines * 11 + 10)
        End Function

        Private Shared Function ContentHeight(row As MatrixRow, font As XFont, labelWidth As Double, valueWidth As Double) As Double
            Dim lines = WrapText(row.Label, font, labelWidth - 8).Count
            For Each cell In row.Cells
                lines = Math.Max(lines, WrapText(cell, font, valueWidth - 6).Count)
            Next
            Return Math.Max(22, lines * 10 + 6)
        End Function

        Private Shared Sub DrawWrapped(graphics As XGraphics, value As String, font As XFont, brush As XBrush,
                                       bounds As XRect, centered As Boolean, Optional headerBreak As Boolean = False)
            Dim lines = If(headerBreak AndAlso HasHeaderBreak(value),
                           HeaderParts(value),
                           WrapText(value, font, bounds.Width))
            Dim lineHeight = 10.0
            Dim startY = bounds.Y + Math.Max(0, (bounds.Height - lines.Count * lineHeight) / 2)
            For i = 0 To lines.Count - 1
                Dim lineBounds = New XRect(bounds.X, startY + i * lineHeight, bounds.Width, lineHeight)
                graphics.DrawString(lines(i), font, brush, lineBounds,
                                    If(centered, XStringFormats.Center, XStringFormats.CenterLeft))
            Next
        End Sub

        Private Shared Function HeaderLineCount(value As String, font As XFont, width As Double) As Integer
            If HasHeaderBreak(value) Then
                Return HeaderParts(value).Count
            End If
            Return WrapText(value, font, width).Count
        End Function

        Private Shared Function HasHeaderBreak(value As String) As Boolean
            Return value.Contains(" ") OrElse value.Contains(vbCr) OrElse value.Contains(vbLf)
        End Function

        Private Shared Function HeaderParts(value As String) As List(Of String)
            Return value.Replace(vbCr, " ").Replace(vbLf, " ").
                Split(" "c).
                Where(Function(part) Not String.IsNullOrWhiteSpace(part)).
                ToList()
        End Function

        Private Shared Function WrapText(value As String, font As XFont, width As Double) As List(Of String)
            Dim result As New List(Of String)()
            If String.IsNullOrWhiteSpace(value) Then
                result.Add("")
                Return result
            End If

            Dim current = ""
            For Each word In value.Split(" "c)
                Dim candidate = If(current = "", word, current & " " & word)
                If current <> "" AndAlso candidate.Length * font.Size * 0.55 > width Then
                    result.Add(current)
                    current = word
                Else
                    current = candidate
                End If
            Next
            If current <> "" Then result.Add(current)
            Return result
        End Function

        Private Shared Function DisplayFilter(value As String) As String
            Return If(String.IsNullOrWhiteSpace(value), "ALL RECORDS", value)
        End Function

        Private Shared Function SafeFileName(value As String) As String
            For Each invalid In IO.Path.GetInvalidFileNameChars()
                value = value.Replace(invalid, "-"c)
            Next
            Return value
        End Function

    End Class
End Namespace
