Imports System.Runtime.CompilerServices

Module Extensions
    <Extension()>
    Public Function CircleCenter(r As Rectangle) As Point
        Return New Point(r.X + r.Width / 2, r.Y + r.Height / 2)
    End Function

    <Extension()>
    Public Function CircleRadius(r As Rectangle) As Integer
        Return r.Width / 2
    End Function

    <Extension()>
    Public Function CircleContainsPoint(r As Rectangle, p As Point) As Boolean
        Dim c = r.CircleCenter()

        Return (p.X - c.X) ^ 2 + (p.Y - c.Y) ^ 2 <= (r.Width / 2) ^ 2
    End Function

    <Extension()>
    Public Function PolygonArea(points As List(Of Point), height As Integer) As Double
        Dim a As Double = 0
        Dim j As Integer
        Dim n = points.Count
        For i = 0 To n - 1
            j = (i + 1) Mod n
            a += points(i).X * (height - points(j).Y)
            a -= (height - points(i).Y) * points(j).X
        Next

        Return a
    End Function

    <Extension()>
    Public Function PolygonCentroid(points As List(Of Point), height As Integer) As Point
        Dim c As Point = Point.Empty

        c.X = points.Average(Function(k) k.X)
        c.Y = points.Average(Function(k) k.Y)

        Return c
    End Function
End Module
