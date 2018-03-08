Imports System.ComponentModel
Imports System.Threading

Public Class CameraViewer
    Implements IDisposable

    Private mCameraPlug As CameraPlug

    Private cancelThreads As Boolean
    Private isDragging As Boolean
    Private initialMousePosition As Point
    Private currentMousePosition As Point
    Private movingThread As New Thread(AddressOf MovingSub)

    Private Sub Camera_Load(sender As System.Object, e As EventArgs) Handles MyBase.Load
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint, True)
        Me.SetStyle(ControlStyles.OptimizedDoubleBuffer, True)
        Me.SetStyle(ControlStyles.UserPaint, True)
        Me.SetStyle(ControlStyles.SupportsTransparentBackColor, False)
        Me.SetStyle(ControlStyles.Opaque, True)

        movingThread = New Thread(AddressOf MovingSub)
        movingThread.Start()
    End Sub

    <Browsable(False),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
        EditorBrowsable(EditorBrowsableState.Never)>
    Public Property CameraPlug() As CameraPlug
        Get
            Return mCameraPlug
        End Get
        Set(value As CameraPlug)
            If mCameraPlug IsNot Nothing Then RemoveHandler mCameraPlug.NewFrame, AddressOf RenderFrame
            mCameraPlug = value
            If mCameraPlug IsNot Nothing Then
                AddHandler mCameraPlug.NewFrame, AddressOf RenderFrame
                If MyBase.AutoSize Then Me.Size = mCameraPlug.Resolution.ToSize
            End If
        End Set
    End Property

    Private Sub RenderFrame(sender As Object, image As Image)
        Me.Invalidate()
    End Sub

    Private Sub CameraViewer_MouseDown(sender As Object, e As MouseEventArgs) Handles Me.MouseDown
        initialMousePosition = e.Location
        currentMousePosition = e.Location
        isDragging = True
    End Sub

    Private Sub CameraViewer_MouseMove(sender As Object, e As MouseEventArgs) Handles Me.MouseMove
        If isDragging Then currentMousePosition = e.Location
    End Sub

    Private Sub CameraViewer_MouseUp(sender As Object, e As MouseEventArgs) Handles Me.MouseUp
        isDragging = False
    End Sub

    Private Sub MovingSub()
        Do
            If isDragging Then
                Dim d As CameraPlug.Directions
                Dim s As Integer

                s = Math.Min(Math.Abs(currentMousePosition.X - initialMousePosition.X) / 2, 10)
                If s > 3 Then
                    If currentMousePosition.X < initialMousePosition.X Then
                        d = CameraPlug.Directions.Right
                    ElseIf currentMousePosition.X > initialMousePosition.X Then
                        d = CameraPlug.Directions.Left
                    End If
                    mCameraPlug.Move(d, s)
                End If

                s = Math.Min(Math.Abs(currentMousePosition.Y - initialMousePosition.Y) / 2, 10)
                If s > 3 Then
                    If currentMousePosition.Y < initialMousePosition.Y Then
                        d = CameraPlug.Directions.Up
                    ElseIf currentMousePosition.Y > initialMousePosition.Y Then
                        d = CameraPlug.Directions.Down
                    End If
                    mCameraPlug.Move(d, s)
                End If
            End If

            Thread.Sleep(10)
        Loop Until cancelThreads
    End Sub

    Private Sub CameraViewer_Paint(sender As Object, e As PaintEventArgs) Handles Me.Paint
        If mCameraPlug Is Nothing Then Exit Sub

        Dim bmp As Bitmap = mCameraPlug.LastFrame

        Dim w As Integer = bmp.Width
        Dim h As Integer = bmp.Height

        If w = 1 Then Exit Sub

        If Me.AutoSize Then
            e.Graphics.DrawImageUnscaled(bmp, 0, 0)
        Else
            e.Graphics.DrawImage(bmp, 0, 0, Me.Width, Me.Height)
        End If

        If isDragging Then
            Using p As New Pen(Color.FromArgb(128, Color.Red), 4)
                e.Graphics.DrawLine(p, initialMousePosition, currentMousePosition)
            End Using
        End If
    End Sub
End Class
