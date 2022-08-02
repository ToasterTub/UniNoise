using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class UniNoiseTexturePreview : EditorWindow
{
    static Texture2D texture;
    public static UniNoiseTexturePreview main;
    static Vector2 offset = Vector2.zero;
    static float scale = 4f;
    static bool fastUpdate = true;
    static bool drawOutline = true;
    Material glMat;
    Texture2D whitePixel;
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(UniNoiseTexturePreview));
    }

    public static void closeWindow()
    {
        main.Close();
    }

    public static void changeTexture(Texture2D tex)
    {
        texture = tex;
    }

    private void OnEnable()
    {
        getPixelTex();
        getMat();
        offset = Vector2.zero;
        main = this;
    }

    /// <summary>
    /// get a blank white texture of 1x1 size
    /// </summary>
    void getPixelTex()
    {
        if (whitePixel == null)
        {
            whitePixel = new Texture2D(1, 1);
            whitePixel.filterMode = FilterMode.Point;
            whitePixel.SetPixel(0, 0, Color.white);
            whitePixel.Apply();
        }
    }

    /// <summary>
    /// Material for rendering the outlined tileable area
    /// </summary>
    void getMat()
    {
        var shader = Shader.Find("Hidden/Internal-Colored");
        glMat = new Material(shader);
    }

    private void OnDisable()
    {
        main = null;
    }

    /// <summary>
    /// Always updates if fastUpdate is on, I don't remember why. I guess it was doing something I didn't like.
    /// </summary>
    private void Update()
    {
        if (fastUpdate)
        {
            Repaint();
        }
    }

    /// <summary>
    /// Update on inspector changes
    /// </summary>
    private void OnInspectorUpdate()
    {
        if (!fastUpdate)
        {
            Repaint();
        }
    }

    private void OnGUI()
    {
        if (!glMat)
        {
            getMat();
        }

        if (texture) {
            if (whitePixel == null)
            {
                getPixelTex();
            }

            Rect texPos = new Rect(0, 0, 2048, 2048);
            Rect view = new Rect(offset.x, offset.y, scale, scale);
            GUI.DrawTextureWithTexCoords(texPos, texture, view, false);

            Rect topBar = new Rect(0, 0, position.width, 32);
            GUI.color = new Color(0, 0, 0, .3f);
            GUI.DrawTexture(topBar, whitePixel);
            GUI.color = Color.white;

            Rect labelRect = new Rect(16, 8, 256, 32);
            GUI.color = Color.yellow;
            GUI.Label(labelRect, "scale = " + ((2048/(float)texture.width)/scale).ToString("N2"), EditorStyles.whiteLabel);

            /// Toggle buttons
            fastUpdate = toggleAndTitle(new Vector2(position.width - 128, 0), "Fast Update", fastUpdate);
            drawOutline = toggleAndTitle(new Vector2(position.width - 256, 0), "Draw Outline", drawOutline);



            if (drawOutline)
            {
                drawSize();
            }
            GUI.color = Color.white;
            

            Event e = Event.current;
            moveView(e);
            zoom(e);
        }
    }

    /// <summary>
    /// Shows a toggle at a position with a name/title, returns if the toggle is on or not
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="title"></param>
    /// <param name="B"></param>
    /// <returns></returns>
    bool toggleAndTitle(Vector2 pos, string title, bool B)
    {
        Rect toggleRect = new Rect(pos.x, pos.y, 64, 32);
        Rect titleRect = new Rect(pos.x + 16, pos.y + 8, 128, 32);
        GUI.Label(titleRect, title, EditorStyles.whiteLabel);
        return GUI.Toggle(toggleRect, B, "");
    }

    /// <summary>
    /// Draw the swuare showing the tileable area
    /// </summary>
    void drawSize()
    {
        GL.Flush();
        float S = ((texture.width)/scale) * (2048/texture.width);

        float offsetX = (position.width - S)/ 2f;
        float offsetY = (position.height - S) / 2f;

        GUI.BeginClip(new Rect(offsetX, offsetY, S, S));

        GL.Clear(true, false, Color.black);
        glMat.SetPass(0);

        GL.Begin(GL.LINES);
        GL.Color(Color.cyan);
        GL.Vertex(new Vector3(0, 0, 0));
        GL.Vertex(new Vector3(S, 0, 0));
        GL.Vertex(new Vector3(S, 0, 0));
        GL.Vertex(new Vector3(S, S, 0));
        GL.Vertex(new Vector3(S, S, 0));
        GL.Vertex(new Vector3(0, S, 0));
        GL.Vertex(new Vector3(0, S, 0));
        GL.Vertex(new Vector3(0, 0, 0));
        GL.End();
        GUI.EndClip();
    }

    /// <summary>
    /// Zoom in and out on the image.
    /// </summary>
    /// <param name="e"></param>
    void zoom(Event e)
    {
        if (e.type == EventType.ScrollWheel)
        {
            float scroll = e.delta.y / 3f;

            doZoom(scroll);
            Repaint();
        }
    }

    /// <summary>
    /// Actually... Do the zoom... Could definitely not be merged with the zoom method. 
    /// This one, this is the one that needed to be seperated.
    /// </summary>
    /// <param name="scroll"></param>
    void doZoom(float scroll)
    {
        float difference = (.5f / (2048 / (float)texture.width)) * scroll;
        scale += difference;
        offset += (new Vector2(0,1) * -difference);
        scale = Mathf.Clamp(scale, .5f, 10f);
    }

    /// <summary>
    /// Drag the preview area around.
    /// </summary>
    bool dragging = false;
    Vector2 lastMouse;
    void moveView(Event e)
    {

        if (e.type == EventType.MouseDown)
        {
            dragging = true;
            lastMouse = e.mousePosition;
        }

        if (dragging)
        {
            Vector2 difference = lastMouse - e.mousePosition;
            difference.x *= -1;

            offset -= (difference/2048f) * scale;

            lastMouse = e.mousePosition;

            Repaint();
        }

        if (e.type == EventType.MouseUp)
        {
            dragging = false;
        }
    }
}
