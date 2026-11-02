Imports System
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Globalization
Imports System.Linq
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Data

Namespace SidhawanForProfsAndAdmins
    Public Partial Module Pages
        Private Sub LoadDepartmentsFromDatabase()
            Data.Departments.Clear()

            For Each department In Database.LoadDepartments()
                Data.Departments.Add(department)
            Next
        End Sub

        ' Admin Information
        Public Function AdminInfo() As UserControl
            Return New SidhawanForProfsAndAdmins.PersonView(
                "Admin Information",
                "Employee Number",
                Data.Admin,
                canEdit:=True)
        End Function

        ' Students
        Public Function AdminStudents() As UserControl
            Dim p = New SidhawanForProfsAndAdmins.ListPage("Students")

            Dim departmentOptions As New ObservableCollection(Of String) From {
                "College of Computer Studies",
                "College of Nursing",
                "College of Education",
                "College of Engineering",
                "College of Business and Accountancy",
                "College of International Hospitality Management",
                "College of Arts and Sciences"
            }

            Dim programOptions As New ObservableCollection(Of String) From {
                "BSIT",
                "BSCS"
            }

            Dim schoolYearOptions As New ObservableCollection(Of String) From {
                "2025-2026",
                "2026-2027"
            }

            Dim levelOptions As New ObservableCollection(Of String) From {
                "First Year",
                "Second Year",
                "Third Year",
                "Fourth Year"
            }

            Dim sectionOptions As New ObservableCollection(Of String) From {
                "BSIT 1A",
                "BSIT 1B",
                "BSIT 1C",
                "BSIT 1D"
            }

            Dim department = Ui.Combo(
                "Department",
                248,
                departmentOptions)

            Dim program = Ui.Combo(
                "Program",
                248,
                programOptions)

            Dim schoolYear = Ui.Combo(
                "School Year",
                160,
                schoolYearOptions)

            Dim level = Ui.Combo(
                "Year Level",
                160,
                levelOptions)

            Dim section = Ui.Combo(
                "Section",
                160,
                sectionOptions)

            department.SelectedIndex = -1
            program.SelectedIndex = -1
            schoolYear.SelectedIndex = -1
            level.SelectedIndex = -1
            section.SelectedIndex = -1

            Dim students = Database.LoadAdminStudents()

            Data.AllStudents.Clear()

            For Each student In students
                Data.AllStudents.Add(student)
            Next

            Dim view = New ListCollectionView(Data.AllStudents)

            view.Filter =
                Function(item)
                    Dim student = TryCast(item, StudentRow)
                    If student Is Nothing Then Return False

                    Dim selectedDepartment = Ui.Sel(department)
                    Dim selectedProgram = Ui.Sel(program)
                    Dim selectedSchoolYear = Ui.Sel(schoolYear)
                    Dim selectedLevel = Ui.Sel(level)
                    Dim selectedSection = Ui.Sel(section)

                    If selectedDepartment <> "" AndAlso
                       selectedDepartment <> "All" AndAlso
                       Not String.Equals(
                           student.DepartmentName,
                           selectedDepartment,
                           StringComparison.OrdinalIgnoreCase) Then
                        Return False
                    End If

                    If selectedProgram <> "" AndAlso
                       selectedProgram <> "All" AndAlso
                       Not String.Equals(
                           student.ProgramID,
                           selectedProgram,
                           StringComparison.OrdinalIgnoreCase) Then
                        Return False
                    End If

                    If selectedSchoolYear <> "" AndAlso
                       selectedSchoolYear <> "All" AndAlso
                       Not String.Equals(
                           student.SchoolYear,
                           selectedSchoolYear,
                           StringComparison.OrdinalIgnoreCase) Then
                        Return False
                    End If

                    If selectedLevel <> "" AndAlso
                       selectedLevel <> "All" AndAlso
                       Not String.Equals(
                           student.YearLevel,
                           selectedLevel,
                           StringComparison.OrdinalIgnoreCase) Then
                        Return False
                    End If

                    If selectedSection <> "" AndAlso
                       selectedSection <> "All" AndAlso
                       Not String.Equals(
                           student.Section,
                           selectedSection,
                           StringComparison.OrdinalIgnoreCase) Then
                        Return False
                    End If

                    Return True
                End Function

            Dim refresh As Action =
                Sub()
                    view.Refresh()
                End Sub

            AddHandler department.SelectionChanged,
                Sub(sender, args)
                    refresh()
                End Sub

            AddHandler program.SelectionChanged,
                Sub(sender, args)
                    refresh()
                End Sub

            AddHandler schoolYear.SelectionChanged,
                Sub(sender, args)
                    refresh()
                End Sub

            AddHandler level.SelectionChanged,
                Sub(sender, args)
                    refresh()
                End Sub

            AddHandler section.SelectionChanged,
                Sub(sender, args)
                    refresh()
                End Sub

            Dim addResetBehavior As Action(Of ComboBox) =
                Sub(combo)
                    Dim valueBeforeOpen As Object = Nothing

                    AddHandler combo.DropDownOpened,
                        Sub(sender, args)
                            valueBeforeOpen = combo.SelectedItem
                        End Sub

                    AddHandler combo.DropDownClosed,
                        Sub(sender, args)
                            If valueBeforeOpen IsNot Nothing AndAlso
                               Equals(valueBeforeOpen, combo.SelectedItem) Then

                                combo.SelectedIndex = -1
                            End If
                        End Sub
                End Sub

            addResetBehavior(department)
            addResetBehavior(program)
            addResetBehavior(schoolYear)
            addResetBehavior(level)


            Dim sectionValueBeforeOpen As String = ""

            AddHandler section.DropDownOpened,
                Sub(sender, args)
                    sectionValueBeforeOpen = Ui.Sel(section)
                End Sub

            AddHandler section.DropDownClosed,
                Sub(sender, args)
                    Dim selectedSection = Ui.Sel(section)

                    If sectionValueBeforeOpen = "" OrElse
                       selectedSection <> sectionValueBeforeOpen Then
                        Return
                    End If

                    Dim hasMatchingRows As Boolean = False

                    For Each item In Data.CourseSummaries
                        Dim summary = TryCast(item, CourseRow)

                        If summary IsNot Nothing AndAlso
                           String.Equals(
                               If(summary.Section, "").Trim(),
                               selectedSection.Trim(),
                               StringComparison.OrdinalIgnoreCase) Then

                            hasMatchingRows = True
                            Exit For
                        End If
                    Next

                    'Only reset a repeated value if it has matching database rows.
                    If hasMatchingRows Then
                        section.SelectedIndex = -1
                    End If
                End Sub

            Dim table =
                Tbl.For(view) _
                    .Head("GridHeaderBold") _
                    .HeadH(45) _
                    .RowH(45) _
                    .Col("STUDENT NO.", "StudentNo", 186, left:=True) _
                    .Link(
                        "NAME",
                        "Name",
                        440,
                        CType(
                            Pages.Opens(
                                CType(p, ListPage),
                                Function(row)
                                    Return StudentDetail(CType(row, StudentRow))
                                End Function),
                            Action(Of Object)),
                        left:=True) _
                    .Col("STATUS", "Status", 384) _
                    .Build()

            p.Filter(
                department,
                program,
                schoolYear,
                level,
                section)

            p.Band(
                Function() BandText(Ui.Sel(section)),
                section)

            p.Table(table)

            If Data.AllStudents.Count = 0 Then
                p.Popup.ShowNotice(
                    "cross",
                    "There are no student records available in the system.")
            End If

            Return p
        End Function

        Private Function StudentDetail(studentRow As StudentRow) As UserControl
            Dim student = Database.LoadStudentProfile(studentRow.StudentNo)

            If student Is Nothing Then
                Return New ListPage("Student profile not found") _
                    .BackTo(Function() AdminStudents())
            End If

            Dim title =
                student.LastName &
                ", " &
                student.FirstName &
                " " &
                student.MiddleName &
                "’s Information"

            Return New PersonView(
                title,
                "Student Number",
                student,
                canEdit:=True,
                back:=Function() AdminStudents())
        End Function

        ' Summary of Record
        Public Function AdminSummaryDepartments() As UserControl
            Dim p = New ListPage("Select Department").BackTo(Function() AdminSummary())
            Dim departments As New ObservableCollection(Of DepartmentRow)()

            LoadDepartmentsFromDatabase()

            For Each department In Data.Departments.
                Where(Function(value) Not String.IsNullOrWhiteSpace(value)).
                Distinct(StringComparer.OrdinalIgnoreCase).
                OrderBy(Function(value) value)

                departments.Add(New DepartmentRow With {.Name = department})
            Next

            Dim openSummary As Action(Of Object) =
                Sub(item)
                    Dim department = TryCast(item, DepartmentRow)
                    If department IsNot Nothing Then
                        p.GoTo(AdminSummary(department.Name))
                    End If
                End Sub

            p.Band("SELECT A DEPARTMENT")
            p.Table(
                Tbl.For(departments).
                    Head("GridHeaderBold").
                    HeadH(40).
                    RowH(38).
                    Link("DEPARTMENT", "Name", 760, openSummary, left:=True).
                    Build())

            If departments.Count = 0 Then
                p.Popup.ShowNotice(
                    "cross",
                    "There are no department records available in the system.")
            End If

            Return p
        End Function

        Public Function AdminSummary(
            Optional selectedDepartmentName As String = "") As UserControl
            Dim p = New SidhawanForProfsAndAdmins.ListPage("Summary of Record")

            LoadDepartmentsFromDatabase()

            Dim departmentOptions As New ObservableCollection(Of String)(Data.Departments)

            Dim programOptions As New ObservableCollection(Of String) From {
                "BSIT",
                "BSCS"
            }

            Dim schoolYearOptions As New ObservableCollection(Of String) From {
                "2025-2026",
                "2026-2027"
            }

            Dim levelOptions As New ObservableCollection(Of String) From {
                "First Year",
                "Second Year",
                "Third Year",
                "Fourth Year"
            }

            Dim sectionOptions As New ObservableCollection(Of String) From {
                "BSIT 1A",
                "BSIT 1B",
                "BSIT 1C",
                "BSIT 1D"
            }

            Dim department = Ui.Combo(
                "Department",
                248,
                departmentOptions)

            Dim program = Ui.Combo(
                "Program",
                248,
                programOptions)

            Dim schoolYear = Ui.Combo(
                "School Year",
                160,
                schoolYearOptions)

            Dim level = Ui.Combo(
                "Year Level",
                160,
                levelOptions)

            Dim section = Ui.Combo(
                "Section",
                160,
                sectionOptions)

            Dim summaries = Database.LoadAdminDetailedSummary()

            Data.CourseSummaries.Clear()

            If summaries IsNot Nothing Then
                For Each summary In summaries
                    Data.CourseSummaries.Add(summary)
                Next
            End If

            Dim view = New ListCollectionView(Data.CourseSummaries)
            Dim noRecordNoticeShown As Boolean

            view.Filter =
                Function(item)
                    Dim summary = TryCast(item, CourseRow)
                    If summary Is Nothing Then Return False

                    Dim selectedDepartment = Ui.Sel(department)
                    Dim selectedProgram = Ui.Sel(program)
                    Dim selectedSchoolYear = Ui.Sel(schoolYear)
                    Dim selectedLevel = Ui.Sel(level)
                    Dim selectedSection = Ui.Sel(section)

                    If selectedDepartment <> "" AndAlso
                       Not String.Equals(
                           summary.DepartmentName,
                           selectedDepartment,
                           StringComparison.OrdinalIgnoreCase) Then
                        Return False
                    End If

                    If selectedProgram <> "" AndAlso
                       Not String.Equals(
                           summary.ProgramID,
                           selectedProgram,
                           StringComparison.OrdinalIgnoreCase) Then
                        Return False
                    End If

                    If selectedSchoolYear <> "" AndAlso
                       Not String.Equals(
                           summary.SchoolYear,
                           selectedSchoolYear,
                           StringComparison.OrdinalIgnoreCase) Then
                        Return False
                    End If

                    If selectedLevel <> "" AndAlso
                       Not String.Equals(
                           If(summary.YearLevel, "").Trim(),
                           selectedLevel.Trim(),
                           StringComparison.OrdinalIgnoreCase) Then
                        Return False
                    End If

                    selectedSection = Ui.Sel(section).Trim()

                    If selectedSection <> "" AndAlso
                       Not String.Equals(
                           If(summary.Section, "").Trim(),
                           selectedSection,
                           StringComparison.OrdinalIgnoreCase) Then

                        Return False
                    End If

                    Return True
                End Function

            Dim refresh As Action =
                Sub()
                    view.Refresh()

                    Dim selectedDepartment = Ui.Sel(department)
                    If String.IsNullOrWhiteSpace(selectedDepartment) Then
                        noRecordNoticeShown = False
                    ElseIf view.IsEmpty Then
                        If Not noRecordNoticeShown Then
                            p.Popup.ShowNotice(
                                "cross",
                                "There are no summary records available for the selected department.")
                            noRecordNoticeShown = True
                        End If
                    Else
                        noRecordNoticeShown = False
                    End If
                End Sub

            AddHandler department.SelectionChanged,
                Sub(sender, args)
                    noRecordNoticeShown = False
                    refresh()
                End Sub

            AddHandler program.SelectionChanged,
                Sub(sender, args)
                    refresh()
                End Sub

            AddHandler schoolYear.SelectionChanged,
                Sub(sender, args)
                    refresh()
                End Sub

            AddHandler level.SelectionChanged,
                Sub(sender, args)
                    refresh()
                End Sub

            AddHandler section.SelectionChanged,
                Sub(sender, args)
                    refresh()
                End Sub

            Dim addResetBehavior As Action(Of ComboBox) =
                Sub(combo)
                    Dim valueBeforeOpen As Object = Nothing

                    AddHandler combo.DropDownOpened,
                        Sub(sender, args)
                            valueBeforeOpen = combo.SelectedItem
                        End Sub

                    AddHandler combo.DropDownClosed,
                        Sub(sender, args)
                            If valueBeforeOpen IsNot Nothing AndAlso
                               Equals(valueBeforeOpen, combo.SelectedItem) Then

                                combo.SelectedIndex = -1
                                view.Refresh()
                            End If
                        End Sub
                End Sub

            addResetBehavior(department)
            addResetBehavior(program)
            addResetBehavior(schoolYear)
            addResetBehavior(level)
            addResetBehavior(section)

            If Not String.IsNullOrWhiteSpace(selectedDepartmentName) Then
                department.SelectedItem = selectedDepartmentName
            End If

            Dim table =
                Tbl.For(view) _
                    .Head("GridHeaderBrand") _
                    .HeadH(54) _
                    .RowH(38) _
                    .Col("Course Code", "CourseCode", 242) _
                    .Link(
                        "Course Name",
                        "CourseName",
                        299,
                        CType(
                            Pages.Opens(
                                CType(p, ListPage),
                                Function(row)
                                    Return Pages.AdminSummaryDetail(
                                        CType(row, CourseRow))
                                End Function),
                            Action(Of Object))) _
                    .Col("Professor", "Professor", 285) _
                    .Col("Units Earned", "Units", 201) _
                    .Build

            table.Margin = New Thickness(0, 20, 0, 0)

            p.Filter(
                department,
                program,
                schoolYear,
                level,
                section)

            p.Table(table)

            If Data.CourseSummaries.Count = 0 AndAlso
               Not noRecordNoticeShown Then
                p.Popup.ShowNotice(
                    "cross",
                    "There are no summary records available in the system.")
            End If

            Return p
        End Function

        Private Function AdminSummaryDetail(c As SidhawanForProfsAndAdmins.CourseRow) As UserControl
            Dim p = New SidhawanForProfsAndAdmins.ListPage("Summary of Record").BackTo(Function() AdminSummary())

            Dim classRows As List(Of ProfessorSummaryRow)

            Try
                classRows = ProfessorStudentService.LoadSummaryForClass(c.ClassID)
            Catch ex As Exception
                p.Popup.ShowNotice(
                    "cross",
                    "Unable to load the records for this class." & vbCrLf & ex.Message)
                Return p
            End Try

            Data.SummarySheet.Rows.Clear()

            Dim firstRow = classRows.FirstOrDefault()
            Data.SummarySheet.Rows.Add(MatrixRow.Create(
                "Grading Percentage:",
                If(firstRow Is Nothing, "", firstRow.AttendancePercentage),
                If(firstRow Is Nothing, "", firstRow.QuizzesPercentage),
                If(firstRow Is Nothing, "", firstRow.ClassworkPercentage),
                If(firstRow Is Nothing, "", firstRow.MajorExamPercentage),
                "", "", "", "", ""))
            Data.SummarySheet.Rows(0).Bold = True
            Data.SummarySheet.Rows.Add(MatrixRow.Create(
                "Highest Possible Score:",
                If(firstRow Is Nothing, "", firstRow.AttendanceHighest),
                If(firstRow Is Nothing, "", firstRow.QuizzesHighest),
                If(firstRow Is Nothing, "", firstRow.ClassworkHighest),
                If(firstRow Is Nothing, "", firstRow.MajorExamHighest),
                "", "", "", "", ""))
            Data.SummarySheet.Rows(1).Bold = True

            For Each row In classRows
                Data.SummarySheet.Rows.Add(MatrixRow.Create(
                    row.StudentName,
                    row.Attendance,
                    row.WrittenWork,
                    row.PerformanceTask,
                    row.MajorExam,
                    row.MidtermGrade,
                    row.FinalGrade,
                    row.OverallGrade,
                    row.Gwa,
                    row.Remarks))
            Next

            p.ExportBtn.Visibility = Visibility.Visible
            AddHandler p.ExportBtn.Click,
                Sub(s, e)
                    PdfExportService.ExportSummary(
                        "Grades for " & c.CourseCode,
                        "ALL RECORDS",
                        "ALL RECORDS",
                        c.CourseCode,
                        Data.SummarySheet)
                End Sub

            p.Band("GRADES FOR " & c.CourseCode & " | " & c.CourseName)
            p.Table(Pages.SummaryTable())

            If classRows.Count = 0 Then
                p.Popup.ShowNotice(
                    "cross",
                    "There are no records available for this class.")
            End If

            Return p
        End Function

        ' Professors
        Public Function AdminProfessorDepartments() As UserControl
            Dim p = New ListPage("Select Department").BackTo(Function() AdminProfessors())
            Dim departments As New ObservableCollection(Of DepartmentRow)()

            LoadDepartmentsFromDatabase()

            For Each department In Data.Departments.
                Where(Function(value) Not String.IsNullOrWhiteSpace(value)).
                Distinct(StringComparer.OrdinalIgnoreCase).
                OrderBy(Function(value) value)

                departments.Add(New DepartmentRow With {.Name = department})
            Next

            Dim openProfessors As Action(Of Object) =
                Sub(item)
                    Dim department = TryCast(item, DepartmentRow)
                    If department IsNot Nothing Then
                        p.GoTo(AdminProfessors(department.Name))
                    End If
                End Sub

            p.Band("SELECT A DEPARTMENT")
            p.Table(
                Tbl.For(departments).
                    Head("GridHeaderBold").
                    HeadH(40).
                    RowH(38).
                    Link("DEPARTMENT", "Name", 760, openProfessors, left:=True).
                    Build())

            If departments.Count = 0 Then
                p.Popup.ShowNotice(
                    "cross",
                    "There are no department records available in the system.")
            End If

            Return p
        End Function

        Public Function AdminProfessors(
            Optional selectedDepartmentName As String = "") As UserControl
            Dim p = New SidhawanForProfsAndAdmins.ListPage("Professors")

            Dim department = Ui.Combo(
                "Department",
                285,
                Data.Departments)

            Dim instructors = Database.LoadAdminInstructors()

            Data.FullTime.Clear()
            Data.PartTime.Clear()

            For Each instructor In instructors
                If String.Equals(
                    If(instructor.DepartmentID, "").Trim(),
                    "RO",
                    StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If

                Dim employmentType =
                    If(instructor.EmploymentType, "") _
                        .Trim() _
                        .Replace("-", "") _
                        .Replace("_", "") _
                        .Replace(" ", "") _
                        .ToUpperInvariant()

                If employmentType.StartsWith(
                    "FULLTIME",
                    StringComparison.OrdinalIgnoreCase) Then

                    Data.FullTime.Add(instructor)
                Else
                    Data.PartTime.Add(instructor)
                End If
            Next

            Dim fullView = New ListCollectionView(Data.FullTime)
            Dim partView = New ListCollectionView(Data.PartTime)
            Dim noRecordNoticeShown As Boolean

            Dim filterView As Action =
                Sub()
                    Dim selectedDepartment = Ui.Sel(department)

                    Dim filter As Predicate(Of Object) =
                        Function(item)
                            Dim instructor = TryCast(item, StaffRow)
                            If instructor Is Nothing Then Return False

                            If selectedDepartment = "" Then
                                Return True
                            End If

                            Return String.Equals(
                                       instructor.DepartmentName,
                                       selectedDepartment,
                                       StringComparison.OrdinalIgnoreCase) OrElse
                                   String.Equals(
                                       instructor.DepartmentID,
                                       selectedDepartment,
                                       StringComparison.OrdinalIgnoreCase)
                        End Function

                    fullView.Filter = filter
                    partView.Filter = filter
                    fullView.Refresh()
                    partView.Refresh()

                    If String.IsNullOrWhiteSpace(selectedDepartment) Then
                        noRecordNoticeShown = False
                    ElseIf fullView.IsEmpty AndAlso
                           partView.IsEmpty Then

                        If Not noRecordNoticeShown Then
                            p.Popup.ShowNotice(
                                "cross",
                                "There are no professor records available for the selected department.")
                            noRecordNoticeShown = True
                        End If
                    Else
                        noRecordNoticeShown = False
                    End If
                End Sub

            AddHandler department.SelectionChanged,
                Sub(sender, args)
                    noRecordNoticeShown = False
                    filterView()
                End Sub

            department.SelectedIndex = -1

            If Not String.IsNullOrWhiteSpace(selectedDepartmentName) Then
                department.SelectedItem = selectedDepartmentName
            End If

            Dim staffTable As Func(Of IEnumerable, UIElement) =
                Function(rows)
                    Return Tbl.For(rows) _
                        .Head("GridHeaderBold") _
                        .HeadH(46) _
                        .RowH(46) _
                        .Col(
                            "Employee ID No.",
                            "Id",
                            185,
                            left:=True) _
                        .Link(
                            "NAME",
                            "Name",
                            825,
                            CType(
                                Pages.Opens(
                                    CType(p, ListPage),
                                    Function(row)
                                        Return ProfDetail(CType(row, StaffRow))
                                    End Function),
                                Action(Of Object)),
                            left:=True) _
                        .Build()
                End Function

            Dim fullSection = New SidhawanForProfsAndAdmins.CollapsibleSection()
            fullSection.Title = "FULL TIME"
            fullSection.Body = staffTable(fullView)

            Dim partSection = New SidhawanForProfsAndAdmins.CollapsibleSection()
            partSection.Title = "PART TIME"
            partSection.IsExpanded = False
            partSection.Body = staffTable(partView)

            Dim stack = Ui.Stack(fullSection, partSection)
            stack.Margin = New Thickness(0, 5, 0, 0)

            p.Filter(department)
            p.Table(stack)

            filterView()

            If Data.FullTime.Count + Data.PartTime.Count = 0 AndAlso
               Not noRecordNoticeShown Then
                p.Popup.ShowNotice(
                    "cross",
                    "There are no professor records available in the system.")
            End If

            Return p
        End Function

        Private Function ProfDetail(instructorRow As StaffRow) As UserControl
            Dim instructor =
                Database.LoadInstructorProfile(instructorRow.Id)

            If instructor Is Nothing Then
                Return New ListPage("Professor profile not found") _
                    .BackTo(Function() AdminProfessors())
            End If

            Dim title =
                instructor.LastName &
                ", " &
                instructor.FirstName &
                "’s Information"

            Return New PersonView(
                title,
                "Employee Number",
                instructor,
                canEdit:=True,
                back:=Function() AdminProfessors())
        End Function

        ' History
        Public Function History() As UserControl
            Database.ReloadHistory()

            If Data.History.Count = 0 Then
                System.Windows.MessageBox.Show(
                    "There are no history records for this user.",
                    "No history records",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information)
            End If

            Dim view = New ListCollectionView(Data.History)

            Dim userText = ""
            Dim noUserRecordShown As Boolean
            Dim fromTime As Date? = Nothing
            Dim toTime As Date? = Nothing

            view.Filter = Function(item)
                              Dim row = TryCast(item, HistoryRow)
                              If row Is Nothing Then Return False

                              Dim searchId = If(userText, "").Trim()
                              Dim rowId = If(row.Id, "").Trim()

                              If searchId.Length > 0 AndAlso
                                 Not String.Equals(
                                     rowId,
                                     searchId,
                                     StringComparison.OrdinalIgnoreCase) Then
                                  Return False
                              End If

                              Dim rowTime As DateTime
                              If Not DateTime.TryParseExact(
                                  row.When,
                                  "yyyy-MM-dd HH:mm:ss",
                                  CultureInfo.InvariantCulture,
                                  DateTimeStyles.None,
                                  rowTime) Then
                                  Return False
                              End If

                              If fromTime.HasValue AndAlso
                                 rowTime < fromTime.Value.Date Then
                                  Return False
                              End If

                              If toTime.HasValue AndAlso
                                 rowTime >= toTime.Value.Date.AddDays(1) Then
                                  Return False
                              End If

                              Return True
                          End Function

            Dim sortBy As Action(Of ListSortDirection) = Sub(dir)
                                                             view.SortDescriptions.Clear()
                                                             view.SortDescriptions.Add(New SortDescription("When", dir))
                                                         End Sub

            Dim up = SidhawanForProfsAndAdmins.Ui.IconBtn("sort_up")
            Dim down = SidhawanForProfsAndAdmins.Ui.IconBtn("sort_down")
            AddHandler up.Click, Sub(s, e) sortBy(ListSortDirection.Ascending)
            AddHandler down.Click, Sub(s, e) sortBy(ListSortDirection.Descending)

            Dim fromBox = New SidhawanForProfsAndAdmins.DateTimeBox()
            fromBox.Label = "History from"
            fromBox.Width = 150
            fromBox.Margin = New Thickness(0, 0, 10, 0)

            Dim toBox = New SidhawanForProfsAndAdmins.DateTimeBox()
            toBox.Label = "History to"
            toBox.Width = 150
            toBox.Margin = New Thickness(0, 0, 10, 0)

            AddHandler fromBox.ValueChanged, Sub(v)
                                                 fromTime = v
                                                 view.Refresh()
                                             End Sub
            AddHandler toBox.ValueChanged, Sub(v)
                                               toTime = v
                                               view.Refresh()
                                           End Sub

            Dim tools = New StackPanel()
            tools.Orientation = Orientation.Horizontal
            tools.Margin = New Thickness(0, 10, 0, 0)

            Dim onUserText As Action(Of String) =
                Sub(text)
                    userText = If(text, "").Trim()
                    view.Refresh()

                    If userText.Length = 0 OrElse view.Count > 0 Then
                        noUserRecordShown = False
                    ElseIf Not noUserRecordShown Then
                        noUserRecordShown = True
                        System.Windows.MessageBox.Show(
                            "There are no history records for user " & userText & ".",
                            "No history records",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information)
                    End If
                End Sub
            tools.Children.Add(HistoryBox("icon", "History user", onUserText))
            tools.Children.Add(fromBox)

            Dim arrow = New SidhawanForProfsAndAdmins.Icon()
            arrow.File = "arrow"
            arrow.Width = 16
            arrow.Height = 16
            arrow.VerticalAlignment = VerticalAlignment.Center
            arrow.Margin = New Thickness(0, 0, 10, 0)
            tools.Children.Add(arrow)

            tools.Children.Add(toBox)
            tools.Children.Add(up)
            tools.Children.Add(down)

            Dim table = SidhawanForProfsAndAdmins.Tbl.For(view).Plain().Head("GridHeaderPlain").HeadH(36).RowH(37).Col("ID No.", "Id", 229, left:=True).Col("Date/Time", "When", 386, left:=True).Col("Changes Made", "Change", 387, left:=True).Build()
            table.Margin = New Thickness(0, 60, 0, 0)

            Dim page = New SidhawanForProfsAndAdmins.ListPage("History").Plain()
            page.Tool(tools)
            page.Table(table)
            Return page
        End Function

        Private Function HistoryBox(iconName As String, label As String, onText As Action(Of String)) As Border
            Dim box = New TextBox()
            box.Style = SidhawanForProfsAndAdmins.Ui.S("BareBox")
            box.Margin = New Thickness(28, 0, 4, 0)
            AddHandler box.TextChanged, Sub(s, e)
                                            SidhawanForProfsAndAdmins.Data.RaiseFilterChanged(label, box.Text)
                                            onText(box.Text)
                                        End Sub

            Dim iconImage = New SidhawanForProfsAndAdmins.Icon()
            iconImage.File = iconName
            iconImage.Width = 18
            iconImage.Height = 18
            iconImage.HorizontalAlignment = HorizontalAlignment.Left
            iconImage.Margin = New Thickness(4, 0, 0, 0)

            Dim inner = New Grid()
            inner.Children.Add(iconImage)
            inner.Children.Add(box)

            Dim border = New Border()
            border.Width = 113
            border.Height = 25
            border.Margin = New Thickness(0, 0, 10, 0)
            border.BorderBrush = SidhawanForProfsAndAdmins.Ui.B("BrandDark")
            border.BorderThickness = New Thickness(2)
            border.CornerRadius = New CornerRadius(3)
            border.Child = inner
            Return border
        End Function

        ' Grading Rules and Policy
        Public Function GradingPolicy() As UserControl
            Dim p = New SidhawanForProfsAndAdmins.ListPage("Grading Rules & Policy").Plain()

            Dim lockBtn = SidhawanForProfsAndAdmins.Ui.PillButton("Lock Grade Editing", Double.NaN, 70, 34)
            lockBtn.Margin = New Thickness(0, 0, 0, 30)

            Dim ruleBtn = SidhawanForProfsAndAdmins.Ui.PillButton("Grading Rule Editor", Double.NaN, 70, 34)
            ruleBtn.Margin = New Thickness(0, 0, 0, 30)

            AddHandler lockBtn.Click, Sub(s, e) p.GoTo(LockGradeDepartments())
            AddHandler ruleBtn.Click, Sub(s, e) p.GoTo(GradingRuleDepartments())

            Dim releaseBtn = SidhawanForProfsAndAdmins.Ui.PillButton("Release Grades", Double.NaN, 70, 34)
            releaseBtn.Margin = New Thickness(0, 0, 0, 30)

            AddHandler releaseBtn.Click, Sub(s, e)
                                             Dim onYes As Action = Sub() p.Popup.ShowNotice("check", "Grades have been released successfully!")
                                             p.Popup.ShowConfirm("Release All Grades?", "", "", onYes)
                                         End Sub

            Dim buttons = SidhawanForProfsAndAdmins.Ui.Stack(lockBtn, ruleBtn, releaseBtn)
            buttons.Margin = New Thickness(54, 100, 54, 0)

            p.Table(buttons)
            Return p
        End Function

        Public Function LockGradeDepartments() As UserControl
            Dim p = New ListPage("Select Department").BackTo(Function() GradingPolicy())
            Dim departments As New ObservableCollection(Of DepartmentRow)()

            LoadDepartmentsFromDatabase()

            For Each department In Data.Departments.
                Where(Function(value) Not String.IsNullOrWhiteSpace(value)).
                Distinct(StringComparer.OrdinalIgnoreCase).
                OrderBy(Function(value) value)

                departments.Add(New DepartmentRow With {.Name = department})
            Next

            Dim openLockPage As Action(Of Object) =
                Sub(item)
                    Dim department = TryCast(item, DepartmentRow)
                    If department IsNot Nothing Then
                        p.GoTo(LockGrades(department.Name))
                    End If
                End Sub

            p.Band("SELECT A DEPARTMENT")
            p.Table(
                Tbl.For(departments).
                    Head("GridHeaderBold").
                    HeadH(40).
                    RowH(38).
                    Link("DEPARTMENT", "Name", 760, openLockPage, left:=True).
                    Build())

            If departments.Count = 0 Then
                p.Popup.ShowNotice(
                    "cross",
                    "There are no department records available in the system.")
            End If

            Return p
        End Function

        Public Function GradingRuleDepartments() As UserControl
            Dim p = New ListPage("Select Department").BackTo(Function() GradingPolicy())
            Dim departments As New ObservableCollection(Of DepartmentRow)()

            LoadDepartmentsFromDatabase()

            For Each department In Data.Departments.
                Where(Function(value) Not String.IsNullOrWhiteSpace(value)).
                Distinct(StringComparer.OrdinalIgnoreCase).
                OrderBy(Function(value) value)

                departments.Add(New DepartmentRow With {.Name = department})
            Next

            Dim openEditor As Action(Of Object) =
                Sub(item)
                    Dim department = TryCast(item, DepartmentRow)
                    If department IsNot Nothing Then
                        p.GoTo(RuleEditor(department.Name))
                    End If
                End Sub

            p.Band("SELECT A DEPARTMENT")
            p.Table(
                Tbl.For(departments).
                    Head("GridHeaderBold").
                    HeadH(40).
                    RowH(38).
                    Link("DEPARTMENT", "Name", 760, openEditor, left:=True).
                    Build())

            If departments.Count = 0 Then
                p.Popup.ShowNotice(
                    "cross",
                    "There are no department records available in the system.")
            End If

            Return p
        End Function

        Public Function LockGrades(
            Optional selectedDepartmentName As String = "") As UserControl
            Dim p = New ListPage("Lock Grade Editing") _
                .BackTo(Function() GradingPolicy())

            LoadDepartmentsFromDatabase()

            Dim instructors = Database.LoadAdminGradePermissions()

            Data.FullTime.Clear()
            Data.PartTime.Clear()

            For Each instructor In instructors
                If String.Equals(
                    If(instructor.DepartmentID, "").Trim(),
                    "RO",
                    StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If

                Dim employmentType =
                    If(instructor.EmploymentType, "") _
                        .Trim() _
                        .Replace("-", "") _
                        .Replace("_", "") _
                        .Replace(" ", "") _
                        .ToUpperInvariant()

                If employmentType.StartsWith(
                    "FULLTIME",
                    StringComparison.OrdinalIgnoreCase) Then

                    Data.FullTime.Add(instructor)
                Else
                    Data.PartTime.Add(instructor)
                End If
            Next

            Dim department = Ui.Combo(
                "Department",
                285,
                Data.Departments)

            Dim sort = Ui.Combo(
                "Sort by",
                148,
                Data.SortOptions)

            department.SelectedIndex = -1
            sort.SelectedIndex = 0

            If Not String.IsNullOrWhiteSpace(selectedDepartmentName) Then
                department.SelectedItem = selectedDepartmentName
            End If

            Dim fullView = New ListCollectionView(Data.FullTime)
            Dim partView = New ListCollectionView(Data.PartTime)
            Dim noRecordNoticeShown As Boolean

            Dim refresh As Action =
                Sub()
                    Dim selectedDepartment = Ui.Sel(department).Trim()
                    Dim selectedStatus = Ui.Sel(sort).Trim()

                    Dim filter As Predicate(Of Object) =
                        Function(item)
                            Dim staff = TryCast(item, StaffRow)
                            If staff Is Nothing Then Return False

                            If selectedDepartment <> "" AndAlso
                               Not String.Equals(
                                   staff.DepartmentName,
                                   selectedDepartment,
                                   StringComparison.OrdinalIgnoreCase) AndAlso
                               Not String.Equals(
                                   staff.DepartmentID,
                                   selectedDepartment,
                                   StringComparison.OrdinalIgnoreCase) Then
                                Return False
                            End If

                            Select Case selectedStatus
                                Case "Locked"
                                    Return staff.IsLocked
                                Case "Unlocked"
                                    Return Not staff.IsLocked
                                Case Else
                                    Return True
                            End Select
                        End Function

                    fullView.Filter = filter
                    partView.Filter = filter
                    fullView.Refresh()
                    partView.Refresh()

                    If String.IsNullOrWhiteSpace(selectedDepartment) Then
                        noRecordNoticeShown = False
                    ElseIf fullView.IsEmpty AndAlso
                           partView.IsEmpty Then

                        If Not noRecordNoticeShown Then
                            p.Popup.ShowNotice(
                                "cross",
                                "There are no grade-permission records available for the selected department.")
                            noRecordNoticeShown = True
                        End If
                    Else
                        noRecordNoticeShown = False
                    End If
                End Sub

            AddHandler department.SelectionChanged,
                Sub(sender, args)
                    noRecordNoticeShown = False
                    refresh()
                End Sub

            AddHandler sort.SelectionChanged,
                Sub(sender, args)
                    refresh()
                End Sub

            Dim askPermissionChange As Action(Of StaffRow) =
                Sub(staff)
                    Dim lockTarget = Not staff.IsLocked
                    Dim actionText = If(lockTarget, "Lock", "Unlock")

                    Dim onYes As Action =
                        Sub()
                            Try
                                Database.SetGradePermission(
                                    staff.Id,
                                    staff.DepartmentID,
                                    lockTarget,
                                    Data.Admin.Id)

                                staff.IsLocked = lockTarget
                                refresh()

                                p.Popup.ShowNotice(
                                    If(lockTarget, "lock", "unlock"),
                                    If(
                                        lockTarget,
                                        "Grade editing has been locked!",
                                        "Grade editing has been unlocked!"))
                            Catch ex As Exception
                                p.Popup.ShowNotice(
                                    "cross",
                                    "Could not " & actionText.ToLowerInvariant() &
                                    " grade editing: " & ex.Message)
                            End Try
                        End Sub

                    p.Popup.ShowConfirm(
                        actionText & " grade editing for ",
                        Ui.Pretty(staff.Name),
                        "?",
                        onYes)
                End Sub

            Dim onLockCell As Action(Of Object) =
                Sub(row)
                    Dim staff = TryCast(row, StaffRow)
                    If staff IsNot Nothing Then
                        askPermissionChange(staff)
                    End If
                End Sub

            Dim lockTable As Func(Of ListCollectionView, UIElement) =
                Function(rows)
                    Return Tbl.For(rows) _
                        .Head("GridHeaderBold") _
                        .HeadH(46) _
                        .RowH(46) _
                        .Col(
                            "Employee" & vbLf & "ID No.",
                            "Id",
                            102,
                            left:=True) _
                        .Col(
                            "Name",
                            "Name",
                            654,
                            left:=True) _
                        .Cell(
                            "Editing Permission",
                            "LockCell",
                            254,
                            onLockCell) _
                        .Build()
                End Function

            Dim allLocked =
                instructors.Count > 0 AndAlso
                instructors.All(Function(staff) staff.IsLocked)

            Dim lockAll = Ui.PillButton(
                If(allLocked, "Unlock All Departments", "Lock All Departments"),
                200,
                50,
                16)

            AddHandler lockAll.Click,
                Sub(sender, args)
                    Dim lockTarget = Not allLocked
                    Dim actionText = If(lockTarget, "lock", "unlock")

                    Dim onYes As Action =
                        Sub()
                            Try
                                Database.SetAllGradePermissions(lockTarget, Data.Admin.Id)

                                For Each staff In instructors
                                    staff.IsLocked = lockTarget
                                Next

                                allLocked = lockTarget
                                lockAll.Content = If(
                                    allLocked,
                                    "Unlock All Departments",
                                    "Lock All Departments")

                                refresh()

                                p.Popup.ShowNotice(
                                    If(lockTarget, "lock", "unlock"),
                                    If(
                                        lockTarget,
                                        "Grade editing has been locked for all departments!",
                                        "Grade editing has been unlocked for all departments!"))
                            Catch ex As Exception
                                p.Popup.ShowNotice(
                                    "cross",
                                    "Could not " & actionText &
                                    " grade editing for all departments: " &
                                    ex.Message)
                            End Try
                        End Sub

                    p.Popup.ShowConfirm(
                        "Do you want to " & actionText & " grades for ",
                        "ALL",
                        " departments?",
                        onYes)
                End Sub

            Dim fullSection = New CollapsibleSection()
            fullSection.Title = "FULL TIME"
            fullSection.Body = lockTable(fullView)

            Dim partSection = New CollapsibleSection()
            partSection.Title = "PART TIME"
            partSection.IsExpanded = False
            partSection.Body = lockTable(partView)

            Dim sections = Ui.Stack(fullSection, partSection)
            sections.Margin = New Thickness(0, 12, 0, 0)

            p.Filter(department, sort)
            p.Tool(lockAll)
            p.Table(sections)

            refresh()

            If Data.FullTime.Count + Data.PartTime.Count = 0 Then
                p.Popup.ShowNotice(
                    "cross",
                    "There are no professor grade-permission records available in the system.")
            End If

            Return p
        End Function

        Public Function RuleEditor(Optional selectedDepartmentName As String = "") As UserControl
            Dim pendingRules As New HashSet(Of RuleRow)()
            Dim originalValues As New Dictionary(Of RuleRow, String)()

            LoadDepartmentsFromDatabase()

            Dim p = New SidhawanForProfsAndAdmins.ListPage("Grading Rule Editor") _
                .BackTo(Function() GradingPolicy()) _
                .Pencil(
                    Sub()
                        For Each rule In Data.Rules
                            rule.Percentage = rule.Percentage.Trim().TrimEnd("%"c)
                            originalValues(rule) = rule.Percentage
                        Next
                    End Sub)

            Dim department = Ui.Combo(
                "Department",
                285,
                Data.Departments)

            Dim selectedDepartmentId As Func(Of String) =
                Function()
                    Dim selectedDepartment = Ui.Sel(department).Trim()

                    If String.Equals(
                        selectedDepartment,
                        "College of Computer Studies",
                        StringComparison.OrdinalIgnoreCase) OrElse
                       String.Equals(
                        selectedDepartment,
                        "CCS",
                        StringComparison.OrdinalIgnoreCase) Then

                        Return "CCS"
                    End If

                    Return selectedDepartment
                End Function

            Dim loadRules As Action =
                Sub()
                    Data.Rules.Clear()
                    pendingRules.Clear()
                    originalValues.Clear()

                    Dim selectedDepartment = Ui.Sel(department).Trim()

                    If selectedDepartment = "" Then
                        Return
                    End If

                    For Each rule In Database.LoadGradePercentages(selectedDepartmentId())
                        Data.Rules.Add(rule)
                        originalValues(rule) = rule.Percentage
                    Next

                    If Data.Rules.Count = 0 Then
                        p.Popup.ShowNotice(
                            "cross",
                            "There are no grading rule records available for the selected department.")
                    End If
                End Sub

            Dim table =
                Tbl.For(CType(Data.Rules, IEnumerable)) _
                    .Head("GridHeaderBrand") _
                    .HeadH(40) _
                    .RowH(29) _
                    .Col(
                        "Grading Aspect",
                        "Aspect",
                        505,
                        bold:=True) _
                    .Edit(
                        "Percentage",
                        "Percentage",
                        505,
                        locked:=False) _
                    .Build()

            table.Margin = New Thickness(0, 5, 0, 0)

            AddHandler department.SelectionChanged,
                Sub(sender, args)
                    loadRules()
                End Sub

            If Not String.IsNullOrWhiteSpace(selectedDepartmentName) Then
                department.SelectedItem = selectedDepartmentName
            End If

            Dim onRuleEdited As Action(Of Object, String) =
                Sub(row, column)
                    If Not String.Equals(
                        column,
                        "Percentage",
                        StringComparison.OrdinalIgnoreCase) Then
                        Return
                    End If

                    Dim rule = TryCast(row, RuleRow)
                    If rule Is Nothing Then Return

                    Dim percentage As Decimal
                    Dim text = Convert.ToString(
                        rule.Percentage,
                        CultureInfo.InvariantCulture).Trim().TrimEnd("%"c)

                    If Not Decimal.TryParse(
                        text,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        percentage) OrElse
                       percentage < 0D OrElse
                       percentage > 100D OrElse
                       percentage <> Decimal.Truncate(percentage) Then

                        p.Popup.ShowNotice(
                            "cross",
                            "Enter a grading percentage from 0 to 100. The department total must not exceed 100%.")

                        rule.Percentage = originalValues(rule)
                        Return
                    End If

                    rule.Percentage = percentage.ToString(
                        "0.##",
                        CultureInfo.InvariantCulture)
                    pendingRules.Add(rule)
                End Sub

            AddHandler Data.RowEdited, onRuleEdited
            AddHandler p.Unloaded,
                Sub(sender, args)
                    RemoveHandler Data.RowEdited, onRuleEdited
                End Sub

            p.Filter(department)
            p.Table(table)

            p.ConfirmResult(
                Function()
                    If pendingRules.Count = 0 Then Return True

                    Try
                        Database.UpdateGradePercentages(
                            selectedDepartmentId(),
                            Data.Rules)

                        loadRules()
                        table.Items.Refresh()
                        p.ConfirmNotice("Grading percentages saved.")
                        Return True
                    Catch ex As Exception
                        p.Popup.ShowNotice("cross", ex.Message)
                        Return False
                    End Try
                End Function,
                show:=False)

            Return p
        End Function
    End Module
End Namespace
