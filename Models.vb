Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Linq

Namespace SidhawanForProfsAndAdmins
    Public Class StudentRow
        Public Property StudentNo As String = ""
        Public Property Name As String = ""
        Public Property Status As String = ""
        Public Property MidtermGrade As String = ""
        Public Property FinalGrade As String = ""
        Public Property YearLevel As String = ""
        Public Property ProgramID As String = ""
        Public Property ProgramName As String = ""
        Public Property DepartmentID As String = ""
        Public Property DepartmentName As String = ""
        Public Property SchoolYear As String = ""
        Public Property Semester As String = ""
        Public Property Section As String = ""
        Public Property CourseCode As String = ""
    End Class

    Public Class ProfessorAttendanceRow
        Public Property ClassID As String = ""
        Public Property StudentID As String = ""
        Public Property StudentName As String = ""
        Public Property AttendanceID As String = ""
        Public Property AttendanceDate As String = ""
        Public Property RawClassDate As String = ""
        Public Property Status As String = ""
    End Class

    Public Class ClassRow
        Public Property ClassID As String = ""
        Public Property Course As String = ""
        Public Property Section As String = ""
        Public Property SchoolYear As String = ""
        Public Property Semester As String = ""
    End Class

    Public Class DepartmentRow
        Public Property Name As String = ""
    End Class

    Public Class CourseRow
        Public Property ClassID As String = ""
        Public Property CourseCode As String = ""
        Public Property CourseName As String = ""
        Public Property ProfessorId As String = ""
        Public Property Professor As String = ""
        Public Property Units As String = ""

        Public Property DepartmentID As String = ""
        Public Property DepartmentName As String = ""
        Public Property ProgramID As String = ""
        Public Property ProgramName As String = ""
        Public Property SchoolYear As String = ""
        Public Property Semester As String = ""
        Public Property YearLevel As String = ""
        Public Property Section As String = ""
    End Class

    Public Class GradeRow
        Public Property SemGrade As String = ""
        Public Property Units As String = ""
    End Class

    Public Class HistoryRow
        Public Property Id As String = ""
        Public Property [When] As String = ""
        Public Property Change As String = ""
    End Class

    Public Class RuleRow
        Public Property Aspect As String = ""
        Public Property Percentage As String = ""
    End Class

    Public Class StaffRow
        Implements INotifyPropertyChanged
        Public Property Id As String = ""
        Public Property Name As String = ""

        Private _locked As Boolean
        Public Property IsLocked As Boolean
            Get
                Return _locked
            End Get
            Set(value As Boolean)
                _locked = value
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("IsLocked"))
            End Set
        End Property
        Public Property EmploymentType As String = ""
        Public Property DepartmentID As String = ""
        Public Property DepartmentName As String = ""
        Public Property IsAdmin As Boolean

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    End Class

    Public Class MatrixRow
        Public Property Label As String = ""
        Public Property Cells As ObservableCollection(Of String) = New ObservableCollection(Of String)()
        Public Property Bold As Boolean

        Public Shared Function Create(label As String, ParamArray cells As String()) As MatrixRow
            Dim row = New MatrixRow()
            row.Label = label
            row.Cells = New ObservableCollection(Of String)(cells)
            Return row
        End Function

        Public Shared Function Blank(label As String, count As Integer) As MatrixRow
            Dim row = New MatrixRow()
            row.Label = label
            row.Cells = New ObservableCollection(Of String)(Enumerable.Repeat("", count))
            Return row
        End Function
    End Class

    Public Class Sheet
        Public ReadOnly Property Columns As ObservableCollection(Of String) = New ObservableCollection(Of String)()
        Public ReadOnly Property Rows As ObservableCollection(Of MatrixRow) = New ObservableCollection(Of MatrixRow)()
    End Class

    Public Class Field
        Public Property Label As String = ""
        Public Property Value As String = ""
        Public Sub New()
        End Sub
        Public Sub New(label As String, value As String)
            Me.Label = label
            Me.Value = value
        End Sub
    End Class

    Public Class Person
        Public Property Id As String = ""
        Public Property FirstName As String = ""
        Public Property MiddleName As String = ""
        Public Property LastName As String = ""
        Public Property Suffix As String = ""
        Public Property BirthDate As String = ""
        Public Property Sex As String = ""
        Public Property ContactNumber As String = ""
        Public Property Email As String = ""
        Public ReadOnly Property Academic As List(Of Field) = New List(Of Field)()
    End Class
End Namespace
