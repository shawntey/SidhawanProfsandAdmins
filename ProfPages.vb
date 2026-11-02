Imports System.Windows
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Controls
Imports System.Windows.Threading

Namespace SidhawanForProfsAndAdmins

    Partial Public Module Pages
        Public Function ProfInfo() As UserControl
            Return New SidhawanForProfsAndAdmins.PersonView("Professor Information", "Employee Number", SidhawanForProfsAndAdmins.Data.Prof)
        End Function

        Public Function ProfStudents() As UserControl
            Dim p = New SidhawanForProfsAndAdmins.ListPage("Students")
            ProfessorStudentService.LoadStudentFilters()

            Dim year = SidhawanForProfsAndAdmins.Ui.Combo("School Year", 248, SidhawanForProfsAndAdmins.Data.SchoolYears)
            Dim semester = SidhawanForProfsAndAdmins.Ui.Combo("Semester", 160, SidhawanForProfsAndAdmins.Data.Semesters)
            Dim course = SidhawanForProfsAndAdmins.Ui.Combo("Course", 248, SidhawanForProfsAndAdmins.Data.Courses)
            Dim section = SidhawanForProfsAndAdmins.Ui.Combo("Section", 248, SidhawanForProfsAndAdmins.Data.Sections)

            Dim reload = Sub()
                             ProfessorStudentService.LoadStudents(
                                  SidhawanForProfsAndAdmins.Ui.Sel(CType(year, ComboBox)),
                                  SidhawanForProfsAndAdmins.Ui.Sel(CType(semester, ComboBox)),
                                  SidhawanForProfsAndAdmins.Ui.Sel(CType(course, ComboBox)),
                                  SidhawanForProfsAndAdmins.Ui.Sel(CType(section, ComboBox)))
                         End Sub
            AddHandler year.SelectionChanged, Sub(sender, e) reload()
            AddHandler semester.SelectionChanged, Sub(sender, e) reload()
            AddHandler course.SelectionChanged, Sub(sender, e) reload()
            AddHandler section.SelectionChanged, Sub(sender, e) reload()

            p.Filter(CType(year, ComboBox), CType(semester, ComboBox), CType(course, ComboBox), CType(section, ComboBox)).Band(Function() CStr(SidhawanForProfsAndAdmins.Ui.Sel(CType(section, ComboBox))), CType(section, ComboBox)).Table(SidhawanForProfsAndAdmins.Tbl.For(CType(SidhawanForProfsAndAdmins.Data.ClassStudents, IEnumerable)).Head("GridHeaderBold").HeadH(38).RowH(38).Col("STUDENT NO.", "StudentNo", 136, left:=True).Col("NAME", "Name", 299, left:=True).Col("STATUS", "Status", 142).Edit("MIDTERM GRADE", "MidtermGrade", 234).Edit("FINAL GRADE", "FinalGrade", 215).Build())

            If SidhawanForProfsAndAdmins.Data.ClassStudents.Count = 0 Then
                p.Popup.ShowNotice(
                    "cross",
                    "No student records are currently assigned to your professor account.")
            End If

            Return p
        End Function

        Private Function ProfGradeEncode(c As SidhawanForProfsAndAdmins.ClassRow) As UserControl
            Return Pages.RecordPage($"Grades for {c.Course} – {c.Section}", New Func(Of UserControl)(AddressOf ProfGrades), Sub(p)
                                                                                                                                p.ExportBtn.Visibility = Visibility.Visible
                                                                                                                                AddHandler p.ExportBtn.Click, Sub(sender, e) p.Popup.ShowNotice("export", $"Exported grades for {c.Course} – {c.Section} successfully!")
                                                                                                                                p.Pencil(Sub()

                                                                                                                                         End Sub)
                                                                                                                            End Sub)
        End Function

        Public Function ProfGrades() As UserControl
            Dim p = New SidhawanForProfsAndAdmins.ListPage("Grades")

            Dim classes As List(Of ClassRow)

            Try
                classes = Database.LoadProfessorClasses(Data.Prof.Id)
            Catch ex As Exception
                p.Popup.ShowNotice("cross", "Unable to load your classes." & vbCrLf & ex.Message)
                Return p
            End Try

            Data.Classes.Clear()

            For Each classRow In classes
                Data.Classes.Add(classRow)
            Next

            Dim schoolYearOptions As New ObservableCollection(Of String) From {
                "All"
            }

            For Each yearValue In classes.
                Select(Function(row) row.SchoolYear).
                Where(Function(value) Not String.IsNullOrWhiteSpace(value)).
                Distinct(StringComparer.OrdinalIgnoreCase).
                OrderBy(Function(value) value)

                schoolYearOptions.Add(yearValue)
            Next

            Dim semesterOptions As New ObservableCollection(Of String) From {
                "All"
            }

            For Each semesterValue In classes.
                Select(Function(row) row.Semester).
                Where(Function(value) Not String.IsNullOrWhiteSpace(value)).
                Distinct(StringComparer.OrdinalIgnoreCase).
                OrderBy(Function(value) value)

                semesterOptions.Add(semesterValue)
            Next

            Dim schoolYearCombo = Ui.Combo(
                "School Year",
                248,
                schoolYearOptions)

            Dim semesterCombo = Ui.Combo(
                "Semester",
                248,
                semesterOptions)

            schoolYearCombo.SelectedIndex = -1
            semesterCombo.SelectedIndex = -1

            Dim view = New ListCollectionView(Data.Classes)

            view.Filter =
                Function(item)
                    Dim classRow = TryCast(item, ClassRow)
                    If classRow Is Nothing Then Return False

                    Dim selectedSchoolYear = Ui.Sel(schoolYearCombo)
                    Dim selectedSemester = Ui.Sel(semesterCombo)

                    Return (selectedSchoolYear = "" OrElse
                            selectedSchoolYear = "All" OrElse
                            String.Equals(
                                classRow.SchoolYear,
                                selectedSchoolYear,
                                StringComparison.OrdinalIgnoreCase)) AndAlso
                           (selectedSemester = "" OrElse
                            selectedSemester = "All" OrElse
                            String.Equals(
                                classRow.Semester,
                                selectedSemester,
                                StringComparison.OrdinalIgnoreCase))
                End Function

            AddHandler schoolYearCombo.SelectionChanged,
                Sub(sender, args)
                    view.Refresh()
                End Sub

            AddHandler semesterCombo.SelectionChanged,
                Sub(sender, args)
                    view.Refresh()
                End Sub

            Dim openClass As Action(Of Object) =
                Sub(item)
                    Dim classRow = TryCast(item, ClassRow)
                    If classRow IsNot Nothing Then
                        p.GoTo(ProfGradeDetail(classRow))
                    End If
                End Sub

            p.Table(
                SidhawanForProfsAndAdmins.Tbl.For(
                    view).
                    Head("GridHeaderBold").
                    HeadH(38).
                    RowH(38).
                    Col("COURSE", "Course", 260, left:=True).
                    Link("SECTION", "Section", 180, openClass).
                    Build())

            p.Filter(schoolYearCombo, semesterCombo)
            p.Band(
                Function() ProfGradesBandText(Ui.Sel(schoolYearCombo), Ui.Sel(semesterCombo)),
                schoolYearCombo,
                semesterCombo)

            If classes.Count = 0 Then
                p.Popup.ShowNotice(
                    "cross",
                    "No class records are currently assigned to your professor account.")
            End If

            Return p
        End Function

        Private Function ProfGradeDetail(classRow As ClassRow) As UserControl
            Dim p = New ListPage(
                $"Grades for {classRow.Course} – {classRow.Section}").
                BackTo(Function() ProfGrades())

            Dim backend As New Backend_Prof_GradeEdit()
            Dim allRows As List(Of ProfessorItemizedGradeRow)
            Dim attendanceRows As List(Of ProfessorAttendanceRow)

            Try
                allRows = backend.LoadGrades(classRow.ClassID)
                attendanceRows = Database.LoadProfessorAttendance(classRow.ClassID)
            Catch ex As Exception
                p.Popup.ShowNotice("cross", "Unable to load the grades for this class." & vbCrLf & ex.Message)
                Return p
            End Try

            Dim classRecordOptions As New ObservableCollection(Of String) From {
                "All", "Attendance", "Classwork", "Quizzes", "Major Examination"}
            Dim gradingPeriodOptions As New ObservableCollection(Of String) From {
                "All", "Midterm", "Finals"}
            Dim itemOptions As New ObservableCollection(Of String) From {"All"}
            Dim gradeItemOptions As New List(Of String) From {"All"}

            For Each value In backend.GetItems(allRows)
                itemOptions.Add(value)
                gradeItemOptions.Add(value)
            Next

            Dim classRecord = Ui.Combo("Class Record", 220, classRecordOptions)
            Dim gradingPeriod = Ui.Combo("Grading Period", 180, gradingPeriodOptions)
            Dim item = Ui.Combo("Class Item", 220, itemOptions)

            classRecord.SelectedIndex = -1
            gradingPeriod.SelectedIndex = -1
            item.SelectedIndex = -1

            Dim rows = New ObservableCollection(Of ProfessorItemizedGradeRow)(allRows)
            Dim view = New ListCollectionView(rows)
            Dim gradeTable As DataGrid = Nothing
            Dim attendanceTable As DataGrid = Nothing
            Dim lastValidScores As New Dictionary(Of ProfessorItemizedGradeRow, String)()
            Dim savedScores As New Dictionary(Of ProfessorItemizedGradeRow, String)()

            For Each row In allRows
                Dim score = If(String.IsNullOrWhiteSpace(row.AcquiredScore), "0", row.AcquiredScore)
                row.AcquiredScore = score
                lastValidScores(row) = score
                savedScores(row) = score
            Next

            Dim attendanceDates = attendanceRows.Select(Function(row) row.AttendanceDate).
        Where(Function(value) Not String.IsNullOrWhiteSpace(value)).
        Distinct(StringComparer.OrdinalIgnoreCase).
        ToList()

            Dim attendanceMonthOptions As New List(Of String) From {"All"}
            For Each attendanceMonth In attendanceDates.
                Select(Function(value) DateTime.ParseExact(value, "MM/dd/yy", CultureInfo.InvariantCulture).ToString("MMMM yyyy", CultureInfo.InvariantCulture)).
                Distinct(StringComparer.OrdinalIgnoreCase).
                OrderBy(Function(value) DateTime.ParseExact(value, "MMMM yyyy", CultureInfo.InvariantCulture))

                attendanceMonthOptions.Add(attendanceMonth)
            Next

            Dim attendanceMatrixRows As New ObservableCollection(Of AttendanceMatrixRow)()
            For Each studentGroup In attendanceRows.GroupBy(Function(row) row.StudentID).
        OrderBy(Function(group) group.First().StudentName)

                Dim matrixRow As New AttendanceMatrixRow With {
            .StudentID = studentGroup.Key,
            .Label = studentGroup.First().StudentName
        }

                For Each dateText In attendanceDates
                    Dim attendance = studentGroup.FirstOrDefault(
                Function(row) String.Equals(row.AttendanceDate, dateText, StringComparison.OrdinalIgnoreCase))

                    matrixRow.Cells.Add(If(attendance Is Nothing, "", attendance.Status))
                    matrixRow.AttendanceIds.Add(If(attendance Is Nothing, "", attendance.AttendanceID))
                Next

                attendanceMatrixRows.Add(matrixRow)
            Next

            Dim pendingEdits As New List(Of Action)()
            Dim lastValidAttendance As New Dictionary(Of String, String)()
            Dim gradeEditing As Boolean
            Dim attendanceEditing As Boolean
            Dim editInteraction As Boolean
            Dim tableContainer = New Grid()
            Dim refreshGradeTable As Action(Of String) = Nothing
            Dim commitActiveEdits As Action = Nothing
            Dim normalGrid = Tbl.For(view).
        Head("GridHeaderBold").
        HeadH(38).
        RowH(38).
        Col("STUDENT", "StudentName", 260, left:=True).
        Col("ITEM", "ItemName", 220, left:=True).
        Col("HIGHEST", "HighestScore", 120).
        Edit("ACQUIRED", "AcquiredScore", 140, locked:=True).
        Col("WEIGHT", "ComponentWeight", 120).
        HeaderDoubleClick(
            Sub(header)
                If Not String.Equals(header, "ACQUIRED", StringComparison.OrdinalIgnoreCase) OrElse
                   Not gradeEditing Then Return

                Dim changed = 0
                For Each row In rows
                    Dim perfectScore = If(row.HighestScore, "").Trim()
                    If String.IsNullOrWhiteSpace(perfectScore) Then Continue For

                    row.AcquiredScore = perfectScore
                    lastValidScores(row) = perfectScore
                    Dim pendingRow = row
                    Dim pendingValue = perfectScore
                    pendingEdits.Add(
                        Sub()
                            backend.SaveScore(pendingRow, pendingValue)
                        End Sub)
                    changed += 1
                Next

                refreshGradeTable($"Perfect scores staged for {changed} row(s). Confirm the edit to save them.")
            End Sub).
        HeaderTripleClick(
            Sub(header)
                If Not String.Equals(header, "ACQUIRED", StringComparison.OrdinalIgnoreCase) OrElse
                   Not gradeEditing Then Return

                Dim changed = 0
                For Each row In rows
                    row.AcquiredScore = "0"
                    lastValidScores(row) = "0"
                    Dim pendingRow = row
                    pendingEdits.Add(
                        Sub()
                            backend.SaveScore(pendingRow, "0")
                        End Sub)
                    changed += 1
                Next

                refreshGradeTable($"Zero scores staged for {changed} row(s). Confirm the edit to save them.")
            End Sub).
        Build()
            gradeTable = normalGrid
            tableContainer.Children.Add(gradeTable)
            refreshGradeTable =
                Sub(message)
                    p.Popup.ShowNotice("check", message)
                End Sub

            If attendanceTable IsNot Nothing Then
                AddHandler attendanceTable.BeginningEdit,
                    Sub(sender, args)
                        editInteraction = True
                    End Sub
            End If

            AddHandler gradeTable.BeginningEdit,
                Sub(sender, args)
                    editInteraction = True
                End Sub

            If attendanceDates.Count > 0 Then
                Dim dateWidths = Enumerable.Repeat(140.0R, attendanceDates.Count).ToArray()
                attendanceTable = Tbl.For(attendanceMatrixRows).
            Head("GridHeaderBold").
            HeadH(54).
            RowH(46).
            Matrix("NAME", 260, attendanceDates, dateWidths, edit:=True, choices:=New String() {"", "Absent", "Late", "Present"}).
            HeaderDoubleClick(
                Sub(header)
                    If Not attendanceEditing Then Return
                    Dim dateIndex = attendanceDates.IndexOf(header)
                    If dateIndex < 0 Then Return

                    Dim changed = 0
                    For Each matrixRow In attendanceMatrixRows
                        Dim attendanceId = matrixRow.AttendanceIds(dateIndex)
                        If String.IsNullOrWhiteSpace(attendanceId) Then Continue For

                        matrixRow.Cells(dateIndex) = "Present"
                        lastValidAttendance(matrixRow.StudentID & "|" & dateIndex.ToString()) = "Present"

                        Dim studentId = matrixRow.StudentID
                        pendingEdits.Add(
                            Sub()
                                Database.UpdateProfessorAttendance(
                                    attendanceId,
                                    studentId,
                                    "Present")
                            End Sub)
                        changed += 1
                    Next

                    p.Popup.ShowNotice("check", $"Present status staged for {changed} row(s). Confirm the edit to save it.")
                End Sub).
            Build()
                Tbl.Lock(attendanceTable)
                attendanceTable.Visibility = Visibility.Collapsed
                tableContainer.Children.Add(attendanceTable)
            End If

            commitActiveEdits =
                Sub()
                    If attendanceTable IsNot Nothing Then
                        attendanceTable.CommitEdit(DataGridEditingUnit.Cell, True)
                        attendanceTable.CommitEdit(DataGridEditingUnit.Row, True)
                    End If

                    If gradeTable IsNot Nothing Then
                        gradeTable.CommitEdit(DataGridEditingUnit.Cell, True)
                        gradeTable.CommitEdit(DataGridEditingUnit.Row, True)
                    End If
                End Sub

            p.Pencil(
                Sub()
                    Try
                        If Database.IsProfessorGradeEditingLocked(Session.UserId) Then
                            System.Windows.MessageBox.Show(
                                "Grade editing is locked for you.",
                                "Grade editing locked",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information)
                            p.CancelEdit()
                            Return
                        End If
                    Catch ex As Exception
                        p.Popup.ShowNotice("cross", "Unable to verify grade editing permissions." & vbCrLf & ex.Message)
                        Return
                    End Try

                    editInteraction = True
                    Data.HasUnsavedEdits = True

                    If String.Equals(Ui.Sel(classRecord), "Attendance", StringComparison.OrdinalIgnoreCase) Then
                        gradeEditing = False
                        If attendanceTable IsNot Nothing Then
                            attendanceEditing = True
                            Tbl.ToggleEdit(attendanceTable)
                        End If
                    Else
                        attendanceEditing = False
                        gradeEditing = True
                        Tbl.ToggleEdit(gradeTable)
                    End If
                End Sub)

            For Each matrixRow In attendanceMatrixRows
                For index = 0 To matrixRow.Cells.Count - 1
                    lastValidAttendance(matrixRow.StudentID & "|" & index.ToString()) = matrixRow.Cells(index)
                Next
            Next

            Dim savedAttendance As New Dictionary(Of String, String)()
            For Each matrixRow In attendanceMatrixRows
                For index = 0 To matrixRow.Cells.Count - 1
                    savedAttendance(matrixRow.StudentID & "|" & index.ToString()) = matrixRow.Cells(index)
                Next
            Next

            Dim hasUnsavedChanges As Func(Of Boolean) =
                Function()
                    If pendingEdits.Count > 0 OrElse editInteraction OrElse Data.HasUnsavedEdits Then Return True

                    For Each row In allRows
                        If Not String.Equals(row.AcquiredScore, savedScores(row), StringComparison.Ordinal) Then
                            Return True
                        End If
                    Next

                    For Each matrixRow In attendanceMatrixRows
                        For index = 0 To matrixRow.Cells.Count - 1
                            Dim key = matrixRow.StudentID & "|" & index.ToString()
                            If Not String.Equals(matrixRow.Cells(index), savedAttendance(key), StringComparison.Ordinal) Then
                                Return True
                            End If
                        Next
                    Next

                    Return False
                End Function

            Dim commitPending As Func(Of Boolean) = Nothing
            Dim discardPending As Action =
                Sub()
                    For Each row In allRows
                        row.AcquiredScore = savedScores(row)
                        lastValidScores(row) = savedScores(row)
                    Next

                    For Each matrixRow In attendanceMatrixRows
                        For index = 0 To matrixRow.Cells.Count - 1
                            Dim key = matrixRow.StudentID & "|" & index.ToString()
                            matrixRow.Cells(index) = savedAttendance(key)
                            lastValidAttendance(key) = savedAttendance(key)
                        Next
                    Next

                    pendingEdits.Clear()
                    Data.PendingEditGuard = Nothing
                    Data.DiscardPendingEdits = Nothing
                    Data.HasUnsavedEdits = False
                    editInteraction = False

                    If attendanceTable IsNot Nothing Then Tbl.Lock(attendanceTable)
                    Tbl.Lock(gradeTable)
                    gradeEditing = False
                    attendanceEditing = False
                    p.CancelEdit()
                End Sub

            commitPending =
                Function()
                    Try
                        For Each saveEdit In pendingEdits.ToList()
                            saveEdit()
                        Next
                        pendingEdits.Clear()
                        Data.PendingEditGuard = Nothing
                        Data.DiscardPendingEdits = Nothing
                        Data.HasUnsavedEdits = False

                        If attendanceTable IsNot Nothing Then Tbl.Lock(attendanceTable)
                        Tbl.Lock(gradeTable)
                        gradeEditing = False
                        attendanceEditing = False
                        editInteraction = False
                        Return True
                    Catch ex As Exception
                        p.Popup.ShowNotice("cross", ex.Message)
                        Return False
                    End Try
                End Function

            Data.PendingEditGuard =
                Function()
                    commitActiveEdits()

                    If Not hasUnsavedChanges() Then
                        Data.PendingEditGuard = Nothing
                        Return True
                    End If

                    Dim answer = MessageBox.Show(
                        "You have unsaved edits. Save them before leaving this page?",
                        "Unsaved edits",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question)

                    If answer = MessageBoxResult.Yes Then Return commitPending()
                    Return False
                End Function

            Data.DiscardPendingEdits = discardPending

            p.ConfirmResult(
                Function()
                    commitActiveEdits()

                    Dim answer = MessageBox.Show(
                        "Do you want to save these edits?",
                        "Confirm edits",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question)

                    If answer = MessageBoxResult.Yes Then
                        Dim saved = commitPending()
                        If saved Then
                            Database.InsertHistory(
                                Session.UserId,
                                Session.UserId &
                                " EDITED GRADE for " &
                                classRow.Course &
                                " - " &
                                classRow.Section &
                                ".")

                            gradeEditing = False
                            attendanceEditing = False
                        End If
                        Return saved
                    End If

                    If answer = MessageBoxResult.No Then
                        discardPending()
                        p.ConfirmNotice("Changes discarded.")
                        gradeEditing = False
                        attendanceEditing = False
                        Return True
                    End If

                    Return False
                End Function,
                show:=False)

            Dim refresh As Action =
                Sub()
                    Dim showAttendance = String.Equals(
                Ui.Sel(classRecord),
                "Attendance",
                StringComparison.OrdinalIgnoreCase)

                    If showAttendance Then
                        If attendanceTable IsNot Nothing Then attendanceTable.Visibility = Visibility.Visible
                        gradeTable.Visibility = Visibility.Collapsed
                    Else
                        If attendanceTable IsNot Nothing Then attendanceTable.Visibility = Visibility.Collapsed
                        gradeTable.Visibility = Visibility.Visible

                        Dim filtered = backend.FilterGrades(
                    allRows,
                    Ui.Sel(gradingPeriod),
                    Ui.Sel(classRecord),
                    Ui.Sel(item))

                        rows.Clear()
                        For Each row In filtered
                            rows.Add(row)
                        Next
                        view.Refresh()
                    End If
                End Sub

            Dim filterChanging As Boolean
            Dim previousClassRecordIndex = classRecord.SelectedIndex
            Dim previousGradingPeriodIndex = gradingPeriod.SelectedIndex
            Dim previousItemIndex = item.SelectedIndex

            AddHandler classRecord.SelectionChanged,
                Sub(sender, args)
                    If filterChanging Then Return

                    Dim requestedIndex = classRecord.SelectedIndex
                    If hasUnsavedChanges() Then
                        discardPending()
                    End If

                    commitActiveEdits()
                    previousClassRecordIndex = classRecord.SelectedIndex
                    attendanceEditing = False

                    filterChanging = True
                    itemOptions.Clear()
                    If String.Equals(Ui.Sel(classRecord), "Attendance", StringComparison.OrdinalIgnoreCase) Then
                        For Each attendanceMonth In attendanceMonthOptions
                            itemOptions.Add(attendanceMonth)
                        Next
                    Else
                        For Each gradeItem In gradeItemOptions
                            itemOptions.Add(gradeItem)
                        Next
                    End If
                    item.SelectedIndex = -1
                    previousItemIndex = item.SelectedIndex
                    filterChanging = False

                    refresh()
                End Sub

            AddHandler gradingPeriod.SelectionChanged,
                Sub(sender, args)
                    If filterChanging Then Return

                    Dim requestedIndex = gradingPeriod.SelectedIndex
                    If hasUnsavedChanges() Then
                        discardPending()
                    End If

                    commitActiveEdits()
                    previousGradingPeriodIndex = gradingPeriod.SelectedIndex
                    refresh()
                End Sub

            AddHandler item.SelectionChanged,
                Sub(sender, args)
                    If filterChanging Then Return

                    Dim requestedIndex = item.SelectedIndex
                    If hasUnsavedChanges() Then
                        discardPending()
                    End If

                    commitActiveEdits()
                    previousItemIndex = item.SelectedIndex

                    If String.Equals(Ui.Sel(classRecord), "Attendance", StringComparison.OrdinalIgnoreCase) AndAlso
                       attendanceTable IsNot Nothing Then

                        Dim selectedMonth = Ui.Sel(item)
                        For index = 0 To attendanceDates.Count - 1
                            Dim showColumn = String.IsNullOrWhiteSpace(selectedMonth) OrElse
                                             String.Equals(selectedMonth, "All", StringComparison.OrdinalIgnoreCase) OrElse
                                             String.Equals(
                                                 DateTime.ParseExact(
                                                     attendanceDates(index),
                                                     "MM/dd/yy",
                                                     CultureInfo.InvariantCulture).ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                                                 selectedMonth,
                                                 StringComparison.OrdinalIgnoreCase)

                            attendanceTable.Columns(index + 1).Visibility = If(showColumn, Visibility.Visible, Visibility.Collapsed)
                        Next
                    End If

                    refresh()
                End Sub

            p.Band(
                Function() If(String.Equals(Ui.Sel(classRecord), "Attendance", StringComparison.OrdinalIgnoreCase),
                      "ATTENDANCE & CLASS PARTICIPATION",
                      GradeBandText(Ui.Sel(classRecord))),
                classRecord)

            p.ExportBtn.Visibility = Visibility.Visible
            AddHandler p.ExportBtn.Click,
                Sub(sender, args)
                    Dim exportRows As IEnumerable(Of ProfessorItemizedGradeRow) = rows
                    If String.Equals(Ui.Sel(classRecord), "Attendance", StringComparison.OrdinalIgnoreCase) Then
                        exportRows = attendanceRows.Select(
                    Function(attendance)
                        Return New ProfessorItemizedGradeRow With {
                            .StudentName = attendance.StudentName,
                            .ItemName = attendance.AttendanceDate,
                            .AcquiredScore = attendance.Status,
                            .Component = "Attendance",
                            .IsAttendance = True
                        }
                    End Function)
                    End If

                    PdfExportService.ExportProfessorGrades(
                $"Grades for {classRow.Course} – {classRow.Section}",
                Ui.Sel(gradingPeriod),
                Ui.Sel(classRecord),
                Ui.Sel(item),
                exportRows)
                End Sub

            Dim onGradeEdited As Action(Of Object, String) =
                Sub(rowObject, column)
                    Dim attendanceRow = TryCast(rowObject, AttendanceMatrixRow)
                    If attendanceRow IsNot Nothing Then
                        Dim dateIndex = attendanceDates.IndexOf(column)
                        If dateIndex < 0 OrElse dateIndex >= attendanceRow.AttendanceIds.Count Then Return

                        Dim attendanceId = attendanceRow.AttendanceIds(dateIndex)
                        If String.IsNullOrWhiteSpace(attendanceId) Then Return

                        Dim attendanceKey = attendanceRow.StudentID & "|" & dateIndex.ToString()
                        Dim previousStatus = If(lastValidAttendance.ContainsKey(attendanceKey),
                                                lastValidAttendance(attendanceKey),
                                                "")
                        Dim status = If(attendanceRow.Cells(dateIndex), "").Trim()
                        If Not IsValidAttendanceStatus(status) Then
                            attendanceRow.Cells(dateIndex) = previousStatus
                            p.Popup.ShowNotice(
                                "cross",
                                "Attendance must be Absent, Present, or Late.")
                            Return
                        End If

                        lastValidAttendance(attendanceKey) = status
                        Dim studentId = attendanceRow.StudentID
                        pendingEdits.Add(
                            Sub()
                                Database.UpdateProfessorAttendance(
                                    attendanceId,
                                    studentId,
                                    status)
                            End Sub)
                        Return
                    End If

                    If Not String.Equals(column, "ACQUIRED", StringComparison.OrdinalIgnoreCase) Then Return

                    Dim row = TryCast(rowObject, ProfessorItemizedGradeRow)
                    If row Is Nothing Then Return

                    Dim previousValue = If(lastValidScores.ContainsKey(row), lastValidScores(row), "0")
                    Try
                        Dim value = If(String.IsNullOrWhiteSpace(row.AcquiredScore), "0", row.AcquiredScore.Trim())
                        row.AcquiredScore = value
                        backend.ValidateScore(row, value)
                        lastValidScores(row) = value
                        pendingEdits.Add(
                            Sub()
                                backend.SaveScore(row, value)
                            End Sub)
                    Catch ex As Exception
                        row.AcquiredScore = previousValue
                        p.Popup.ShowNotice("cross", ex.Message)
                        refresh()
                    End Try
                End Sub

            AddHandler Data.RowEdited, onGradeEdited

            p.Filter(classRecord, gradingPeriod, item)
            p.Table(tableContainer)

            refresh()
            Return p
        End Function

        Private Function IsValidAttendanceStatus(value As String) As Boolean
            Return String.IsNullOrWhiteSpace(value) OrElse
                   String.Equals(value, "Absent", StringComparison.OrdinalIgnoreCase) OrElse
                   String.Equals(value, "Present", StringComparison.OrdinalIgnoreCase) OrElse
                   String.Equals(value, "Late", StringComparison.OrdinalIgnoreCase)
        End Function

        Private Function GradeBandText(component As String) As String
            If String.IsNullOrWhiteSpace(component) OrElse
                String.Equals(component, "All", StringComparison.OrdinalIgnoreCase) Then
                Return "ALL GRADES"
            End If

            Return component.ToUpperInvariant() & " GRADES"
        End Function

        Public Function ProfSummary() As UserControl
            ProfessorStudentService.LoadSummaryFilters()

            Dim year = SidhawanForProfsAndAdmins.Ui.Combo("School Year", 248, SidhawanForProfsAndAdmins.Data.SchoolYears)
            Dim section = SidhawanForProfsAndAdmins.Ui.Combo("Section", 248, SidhawanForProfsAndAdmins.Data.Sections)
            Dim course = SidhawanForProfsAndAdmins.Ui.Combo("Course", 248, SidhawanForProfsAndAdmins.Data.Courses)

            Dim reload = Sub()
                             ProfessorStudentService.LoadSummary(
                                  SidhawanForProfsAndAdmins.Ui.Sel(CType(year, ComboBox)),
                                  SidhawanForProfsAndAdmins.Ui.Sel(CType(section, ComboBox)),
                                  SidhawanForProfsAndAdmins.Ui.Sel(CType(course, ComboBox)))
                         End Sub
            AddHandler year.SelectionChanged, Sub(sender, e) reload()
            AddHandler section.SelectionChanged, Sub(sender, e) reload()
            AddHandler course.SelectionChanged, Sub(sender, e) reload()

            Dim p = New SidhawanForProfsAndAdmins.ListPage("Summary of Record").Filter(CType(year, ComboBox), CType(section, ComboBox), CType(course, ComboBox)).Band(
                Function() SummaryBandText(CType(year, ComboBox), CType(section, ComboBox), CType(course, ComboBox)),
                CType(course, ComboBox), CType(section, ComboBox)).Table(Pages.SummaryTable())

            If SidhawanForProfsAndAdmins.Data.Courses.Count = 0 Then
                p.Popup.ShowNotice(
                    "cross",
                    "No summary records are currently available for your professor account.")
            End If

            p.ExportBtn.Visibility = Visibility.Visible
            AddHandler p.ExportBtn.Click,
                Sub(s, e)
                    PdfExportService.ExportSummary(
                        "Summary of Record",
                        SidhawanForProfsAndAdmins.Ui.Sel(CType(year, ComboBox)),
                        SidhawanForProfsAndAdmins.Ui.Sel(CType(section, ComboBox)),
                        SidhawanForProfsAndAdmins.Ui.Sel(CType(course, ComboBox)),
                        SidhawanForProfsAndAdmins.Data.SummarySheet)
                End Sub
            Return p
        End Function

        Private Function SummaryBandText(year As ComboBox, section As ComboBox, course As ComboBox) As String
            Dim selectedYear = SidhawanForProfsAndAdmins.Ui.Sel(year)
            Dim selectedSection = SidhawanForProfsAndAdmins.Ui.Sel(section)
            Dim selectedCourse = SidhawanForProfsAndAdmins.Ui.Sel(course)

            If String.IsNullOrWhiteSpace(selectedYear) AndAlso
               String.IsNullOrWhiteSpace(selectedSection) AndAlso
               String.IsNullOrWhiteSpace(selectedCourse) Then
                Return "ALL RECORDS"
            End If

            Dim courseText = If(String.IsNullOrWhiteSpace(selectedCourse), "ALL COURSES", SidhawanForProfsAndAdmins.Ui.Code(selectedCourse))
            Dim sectionText = If(String.IsNullOrWhiteSpace(selectedSection), "ALL SECTIONS", selectedSection)
            Return courseText & " - " & sectionText
        End Function

        Private Function ProfGradesBandText(schoolYear As String, semester As String) As String
            Dim values As New List(Of String)()

            If Not String.IsNullOrWhiteSpace(schoolYear) AndAlso
               Not String.Equals(schoolYear, "All", StringComparison.OrdinalIgnoreCase) Then
                values.Add(schoolYear)
            End If

            If Not String.IsNullOrWhiteSpace(semester) AndAlso
               Not String.Equals(semester, "All", StringComparison.OrdinalIgnoreCase) Then
                values.Add(semester)
            End If

            Return If(values.Count = 0, "ALL RECORDS", String.Join(" | ", values))
        End Function

        Private Function BandText(value As String) As String
            Return If(String.IsNullOrWhiteSpace(value), "ALL RECORDS", value)
        End Function
    End Module
End Namespace
