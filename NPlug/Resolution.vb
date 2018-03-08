Public Class Resolution
    Private mWidth As Integer
    Private mHeight As Integer

    Public Sub New()
        mWidth = 0
        mHeight = 0
    End Sub

    Public Sub New(width As Integer, height As Integer)
        mWidth = width
        mHeight = height
    End Sub

    Public Sub New(resolution As String)
        Me.New()
        If resolution.Contains("x") Then
            Dim res() As String = resolution.Split("x"c)
            Integer.TryParse(res(0), mWidth)
            Integer.TryParse(res(1), mHeight)
        End If
    End Sub

    Public Property Width() As Integer
        Get
            Return mWidth
        End Get
        Set(ByVal value As Integer)
            mWidth = value
        End Set
    End Property

    Public Property Height() As Integer
        Get
            Return mHeight
        End Get
        Set(ByVal value As Integer)
            mHeight = value
        End Set
    End Property

    Public ReadOnly Property Value As Integer
        Get
            Select Case mWidth
                Case 160 : Return 2
                Case 320 : Return 8
                Case Else : Return 32
            End Select
        End Get
    End Property

    Public Function ToSize() As Size
        Return New Size(mWidth, mHeight)
    End Function

    Public Overrides Function ToString() As String
        Return String.Format("{0}x{1}", mWidth, mHeight)
    End Function
End Class
