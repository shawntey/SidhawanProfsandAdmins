Imports System
Imports System.Collections.ObjectModel

Namespace SidhawanForProfsAndAdmins

    Public Module Data

        ' dropdown options
        Public ReadOnly Property Departments As ObservableCollection(Of String) = New ObservableCollection(Of String)()
        Public ReadOnly Property Programs As ObservableCollection(Of String) = New ObservableCollection(Of String)()
        Public ReadOnly Property Terms As ObservableCollection(Of String) = New ObservableCollection(Of String)()
        Public ReadOnly Property SchoolYears As ObservableCollection(Of String) = New ObservableCollection(Of String)()
        Public ReadOnly Property Semesters As ObservableCollection(Of String) = New ObservableCollection(Of String)()
        Public ReadOnly Property Levels As ObservableCollection(Of String) = New ObservableCollection(Of String)()
        Public ReadOnly Property Sections As ObservableCollection(Of String) = New ObservableCollection(Of String)()
        Public ReadOnly Property Courses As ObservableCollection(Of String) = New ObservableCollection(Of String)()

        Public ReadOnly RecordKinds As String() = {"Attendance", "Written Work", "Performance Task", "Major Exam"}
        Public ReadOnly EntryCategories As String() = {"Midterms", "Finals"}
        Public ReadOnly SortOptions As String() = {"All", "Locked", "Unlocked"}
        Public ReadOnly ClassItems As String() = {"Quiz 1", "Quiz 2", "Quiz 3"}

        ' information pages
        Public Property Prof As Person = New Person()
        Public Property Admin As Person = New Person()
        Public Property Selected As Person = New Person()

        ' prof tables
        Public ReadOnly Property ClassStudents As ObservableCollection(Of StudentRow) = New ObservableCollection(Of StudentRow)()
        Public ReadOnly Property Classes As ObservableCollection(Of ClassRow) = New ObservableCollection(Of ClassRow)()
        Public ReadOnly Property AttendanceSheet As Sheet = New Sheet()
        Public ReadOnly Property WrittenSheet As Sheet = New Sheet()
        Public ReadOnly Property PerformanceSheet As Sheet = New Sheet()
        Public ReadOnly Property ExamSheet As Sheet = New Sheet()
        Public ReadOnly Property SummarySheet As Sheet = New Sheet()

        ' admin tables
        Public ReadOnly Property AllStudents As ObservableCollection(Of StudentRow) = New ObservableCollection(Of StudentRow)()
        Public ReadOnly Property CourseSummaries As ObservableCollection(Of CourseRow) = New ObservableCollection(Of CourseRow)()
        Public ReadOnly Property FullTime As ObservableCollection(Of StaffRow) = New ObservableCollection(Of StaffRow)()
        Public ReadOnly Property PartTime As ObservableCollection(Of StaffRow) = New ObservableCollection(Of StaffRow)()
        Public ReadOnly Property History As ObservableCollection(Of HistoryRow) = New ObservableCollection(Of HistoryRow)()
        Public ReadOnly Property Rules As ObservableCollection(Of RuleRow) = New ObservableCollection(Of RuleRow)()

        Sub New()
            For Each h In {"MIDTERMS", "FINALS", "TOTAL", "PS", "WS", "INITIAL" & vbLf & "GRADE", "FINAL" & vbLf & "GRADE"}
                ExamSheet.Columns.Add(h)
            Next
            For Each h In {"Attendance", "Quizzes", "Class" & vbLf & "Work",
                           "Major" & vbLf & "Examination", "Midterm" & vbLf & "Grade", "Final" & vbLf & "Grade",
                           "Overall" & vbLf & "Grade", "GWA", "Remarks"}
                SummarySheet.Columns.Add(h)
            Next
        End Sub

        ' events
        Public Event PageRequested As Action(Of Role, String)
        Public Event FilterChanged As Action(Of String, String)
        Public Event RowOpened As Action(Of Object)
        Public Event RowEdited As Action(Of Object, String)

        Public Property PendingEditGuard As Func(Of Boolean)
        Public Property DiscardPendingEdits As Action
        Public Property HasUnsavedEdits As Boolean

        Public Function AllowPendingEditNavigation() As Boolean
            If PendingEditGuard Is Nothing Then Return True
            Return PendingEditGuard.Invoke()
        End Function

        Public Sub Request(role As Role, page As String)
            RaiseEvent PageRequested(role, page)
        End Sub

        Public Sub RaiseFilterChanged(label As String, value As String)
            RaiseEvent FilterChanged(label, value)
        End Sub

        Public Sub Open(row As Object)
            RaiseEvent RowOpened(row)
        End Sub

        Public Sub Edited(row As Object, column As String)
            RaiseEvent RowEdited(row, column)
        End Sub

        Public Sub AddHistory(employeeId As String, changeText As String)
            History.Insert(
                0,
                New HistoryRow With {
                    .Id = employeeId,
                    .When = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    .Change = changeText
                })
        End Sub

    End Module
End Namespace