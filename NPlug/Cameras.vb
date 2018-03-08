Public Class Cameras
    Implements Generic.IList(Of CameraViewer)

    Private mCol As List(Of CameraViewer)

    Public Sub Add(ByVal item As CameraViewer) Implements System.Collections.Generic.ICollection(Of CameraViewer).Add
        mCol.Add(item)
    End Sub

    Public Sub Clear() Implements System.Collections.Generic.ICollection(Of CameraViewer).Clear
        mCol.Clear()
    End Sub

    Public Function Contains(ByVal item As CameraViewer) As Boolean Implements System.Collections.Generic.ICollection(Of CameraViewer).Contains
        Return mCol.Contains(item)
    End Function

    Public Sub CopyTo(ByVal array() As CameraViewer, ByVal arrayIndex As Integer) Implements System.Collections.Generic.ICollection(Of CameraViewer).CopyTo
        mCol.CopyTo(array, arrayIndex)
    End Sub

    Public ReadOnly Property Count() As Integer Implements System.Collections.Generic.ICollection(Of CameraViewer).Count
        Get
            Return mCol.Count
        End Get
    End Property

    Public ReadOnly Property IsReadOnly() As Boolean Implements System.Collections.Generic.ICollection(Of CameraViewer).IsReadOnly
        Get
            Return False
        End Get
    End Property

    Public Function Remove(ByVal item As CameraViewer) As Boolean Implements System.Collections.Generic.ICollection(Of CameraViewer).Remove
        Return mCol.Remove(item)
    End Function

    Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of CameraViewer) Implements System.Collections.Generic.IEnumerable(Of CameraViewer).GetEnumerator
        Return mCol.GetEnumerator
    End Function

    Public Function IndexOf(ByVal item As CameraViewer) As Integer Implements System.Collections.Generic.IList(Of CameraViewer).IndexOf
        Return mCol.IndexOf(item)
    End Function

    Public Sub Insert(ByVal index As Integer, ByVal item As CameraViewer) Implements System.Collections.Generic.IList(Of CameraViewer).Insert
        mCol.Insert(index, item)
    End Sub

    Default Public Property Item(ByVal index As Integer) As CameraViewer Implements System.Collections.Generic.IList(Of CameraViewer).Item
        Get
            Return mCol.Item(index)
        End Get
        Set(ByVal value As CameraViewer)
            mCol.Item(index) = value
        End Set
    End Property

    Public Sub RemoveAt(ByVal index As Integer) Implements System.Collections.Generic.IList(Of CameraViewer).RemoveAt
        mCol.RemoveAt(index)
    End Sub

    Public Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return mCol.GetEnumerator
    End Function
End Class
