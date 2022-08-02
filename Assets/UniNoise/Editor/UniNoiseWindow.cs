using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
//using Unity.EditorCoroutines.Editor;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class UniNoiseWindow : EditorWindow
{
    [MenuItem("Window/UniNoise")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(UniNoiseWindow));
    }

    #region Variable Madness
    #region This is a no judging zone of old code... remember this
    #region I mean, not just this region... kind of.. the whole thing

    string saveLocation = ""; // where the image will be saved to

    public Gradient mainGradient = new Gradient(); // primary selected gradient

    UnityEditor.AnimatedValues.AnimBool typeAB; // for animating dropdowns in the generate panel
    UnityEditor.AnimatedValues.AnimBool gradientsAB;

    

    Texture2D previewTex; // The texture the shader is drawn to, shown as the preview
    [HideInInspector]
    public Texture2D texToEdit; // the main texture selected in the Edit tab

    public int textureSize = 512;
    int mode = 0; // 0 for generate, 1 for edit

    public GenOrEditClass selectedInfo = null;
    public GenOrEditClass selectedEdit = null;

    [HideInInspector]
    public Material blitmat; // the material that draws the generated image
    Material glMat; // used for drawing GL lines

    Vector2 scrollPos = Vector2.zero;
    bool mouseDown = false;
    

    

    public static UniNoiseScriptableInfo info; // scriptable object mainly used for saving presets

    GradientAlphaKey[] baseGradientAlphas; // selected gradient alphas
    public static UniNoiseWindow Main; // this

    static bool information = false; // whether or not to show the information panel

    public static GUIStyle grayBack;

    #endregion
    #endregion
    #endregion

    /// <summary>
    /// get a 1x1 image of color 'c'
    /// </summary>
    Texture2D getBackgroundPixelImage(Color c)
    {
        Texture2D back = new Texture2D(1, 1);
        back.SetPixel(1,1,c);
        back.Apply();
        return back;
    }

    void OnEnable()
    {
        grayBack = new GUIStyle();
        grayBack.normal.background = getBackgroundPixelImage(new Color(.3f,.3f,.35f,1));
        Main = this;

        tryToLoadInfo();
        
        var shader = Shader.Find("Hidden/Internal-Colored");
        glMat = new Material(shader);
        previewTex = null;

        typeAB = new UnityEditor.AnimatedValues.AnimBool(true);
        typeAB.valueChanged.AddListener(Repaint);
        gradientsAB = new UnityEditor.AnimatedValues.AnimBool(false);
        gradientsAB.valueChanged.AddListener(Repaint);
        setupGradient();
    }

    private void OnDisable()
    {
        DestroyImmediate(glMat);
    }

    /// <summary>
    /// Setup default gradient values
    /// </summary>
    void setupGradient()
    {
        baseGradientAlphas = new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) };
        mainGradient.colorKeys = new GradientColorKey[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.white, 1) };
    }

    /// <summary>
    /// Set the main gradient as a random two color gradient
    /// </summary>
    void Color2Gradient()
    {
        float h = Random.Range(0f, 1f);
        float s = Random.Range(0f, 1f);
        float v = Random.Range(0f, 1f);
        float h2 = Random.Range(0f, 1f);
        float s2 = Random.Range(0f, 1f);
        float v2 = Random.Range(0f, 1f);

        GradientColorKey[] cols = new GradientColorKey[] { new GradientColorKey(Color.HSVToRGB(h, s, v), 0), new GradientColorKey(Color.HSVToRGB(h2, s2, v2), 1) };
        mainGradient.SetKeys(cols, baseGradientAlphas);
        Repaint();
        updateSelecteds();
    }

    /// <summary>
    /// Set the main gradient to a random gradient with 2-8 colors
    /// </summary>
    void colorRandomGradient()
    {
        GradientColorKey[] keys = new GradientColorKey[Random.Range(2, 8)];

        for(int i = 0; i < keys.Length; i++)
        {
            float h = Random.Range(0f, 1f);
            float s = Random.Range(0f, 1f);
            float v = Random.Range(0f, 1f);
            keys[i] = new GradientColorKey(Color.HSVToRGB(h, s, v), Random.Range(0f,1f));
        }

        mainGradient.SetKeys(keys, baseGradientAlphas); 
        Repaint();
        updateSelecteds();
    }

    /// <summary>
    /// Create or load the base scriptable object for holding data
    /// </summary>
    void tryToLoadInfo()
    {
        string path = UniNoiseScriptableInfo.defaultSaveLocation();
        UniNoiseScriptableInfo I = AssetDatabase.LoadAssetAtPath(path, typeof(UniNoiseScriptableInfo)) as UniNoiseScriptableInfo;
        if (I == null)
        {
            I = CreateInstance(typeof(UniNoiseScriptableInfo)) as UniNoiseScriptableInfo;
            AssetDatabase.CreateAsset(I, path);
        }

        info = I;
        saveLocation = I.saveLocation;
    }

    /// <summary>
    /// Updates the main scriptable object's save location
    /// </summary>
    void updateInfo()
    {
        if (info != null)
        {
            info.saveLocation = saveLocation;
            EditorUtility.SetDirty(info);
        }
    }
    
    /// <summary>
    /// Applies the given texture as the preview texture.
    /// </summary>
    public void setPreviewTexture(Texture2D T)
    {
        if (previewTex != null)
        {
            DestroyImmediate(previewTex);
        }

        previewTex = T;
        UniNoiseTexturePreview.changeTexture(previewTex);
    }

    /// <summary>
    /// Update the current selected generation/edit type 
    /// </summary>
    void updateSelecteds()
    {
        if (mode == 0)
        {
            if (selectedInfo != null)
            {
                selectedInfo.updateImage();
            }
        }
        else if (mode == 1)
        {
            if (selectedEdit != null)
            {
                selectedEdit.updateImage();
            }
        }
    }
    
    /// <summary>
    /// The drag n' drop folder selection area for the image save location
    /// </summary>
    public void DropAreaGUI(Rect drop_area)
    {
        Event evt = Event.current;

        string buttonText = saveLocation == "" ? "\n DRAG OUTPUT FOLDER HERE" : "SAVE TO : \n" + "Assets/" + saveLocation + "\n drag folder here to change location";

        if (drop_area.Contains(evt.mousePosition) && DragAndDrop.objectReferences.Length > 0)
        {
            GUI.color = Color.black;
            GUI.Box(drop_area, new GUIContent(buttonText, "Default save location is - " + defaulSaveLocation() + "\n click to go to folder") , EditorStyles.centeredGreyMiniLabel);
            GUI.color = Color.white;
        }
        else
        {
            GUI.Box(drop_area, new GUIContent(buttonText, "Default save location is - " + defaulSaveLocation() + "\n click to go to folder") , EditorStyles.centeredGreyMiniLabel);
        }
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!drop_area.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    if (DragAndDrop.objectReferences.Length > 1)
                    {
                        return;
                    }

                    if (Directory.Exists(AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[0])))
                    {
                        saveLocation = AssetDatabase.GetAssetPath(DragAndDrop.objectReferences[0]);
                        saveLocation = saveLocation.Substring(6, saveLocation.Length - 6) + "/";
                        updateInfo();
                    }
                    else
                    {
                        Debug.LogWarning("That's not a folder. You've gotta drag a folder there.");
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Shows the information panel
    /// </summary>
    public void informationGenerate()
    {

        GUI.color = Color.cyan;
        EditorGUILayout.LabelField("Hi!", EditorStyles.centeredGreyMiniLabel);
        GUI.color = Color.white;
        EditorGUILayout.LabelField("\nClick the ⓘ in the upper right of this window to go back to UniNoise.\n"
            , EditorStyles.wordWrappedLabel);

        GUI.color = Color.cyan;
        EditorGUILayout.LabelField("\nGenerate or Edit", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(30));
        GUI.color = Color.white;
        EditorGUILayout.LabelField("\nYou can choose a type of image to generate in the Generate tab. Go to the Edit tab" +
            "to edit images.\n\n" +
            "In the edit tab, you must choose an image to edit before doing anything.\n"
            , EditorStyles.wordWrappedLabel);

        GUI.color = Color.cyan;
        EditorGUILayout.LabelField("\nSaving", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(30));
        GUI.color = Color.white;
        EditorGUILayout.LabelField("\nDrag a folder to the area just under the Generate and Edit tabs to set the folder you want " +
            "the images to be saved in.\n\n" +
            "Clicking that area will select the folder in the Project window.\n\n" +
            "Files are given a default name when saving them.\n"
            , EditorStyles.wordWrappedLabel);

        GUI.color = Color.cyan;
        EditorGUILayout.LabelField("\nSelecting", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(30));
        GUI.color = Color.white;
        EditorGUILayout.LabelField("\nSelect a type of noise/pattern from the Pattern Type dropdown area.\n"
            , EditorStyles.wordWrappedLabel);

        GUI.color = Color.cyan;
        EditorGUILayout.LabelField("\nPopout View", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(30));
        GUI.color = Color.white;
        EditorGUILayout.LabelField("\nClicking Popout View will open a seperate dockable window to view the texture.\n\n" +
            "The blue square in the pop-out view represents the size of the image. This can be turned off with the Draw Outline " +
            "toggle.\n\n" +
            "Fast Update, when on, causes many uneccesary UI updates but will give a more responsize preview.\n"
            , EditorStyles.wordWrappedLabel);

        GUI.color = Color.cyan;
        EditorGUILayout.LabelField("\nPicking Colors", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(30));
        GUI.color = Color.white;
        EditorGUILayout.LabelField("\nChoose colors by editing the gradient below the green save button. When " +
            "selecting a preset, the " +
            "gradient preview isn't updated automatically. The correct gradient will be used.\nClick the gradient preview " +
            "to update the preview.\n"
            , EditorStyles.wordWrappedLabel);

        GUI.color = Color.cyan;
        EditorGUILayout.LabelField("\nRandom Gradients", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(30));
        GUI.color = Color.white;
        EditorGUILayout.LabelField("\nOpen the random gradients dropdown when you don't know what colors you want.\n\n" +
            "2 Color generates a 2 color gradient with one at the beginning and one at the end.\n\n" +
            "Random generates a gradient with 2-8 color keys. Both types have full alpha throughout the gradient.\n"
            , EditorStyles.wordWrappedLabel);

        GUI.color = Color.cyan;
        EditorGUILayout.LabelField("\nPresets", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(30));
        GUI.color = Color.white;
        EditorGUILayout.LabelField("\nThe Preset dropdown below the Reset button has some presets for most generation " +
            "types. Check them out " +
            "to see what can be done with some of the generators.\n\nThe red X deletes that preset, and Replace replaces the " +
            "preset on that row with the current settings.\n\nSaving will add a new preset with a default name, just type in that " +
            "field to change the name.\n\n" +
            "Almost everything is saved in a preset, but not everything. "+
            "Where generators need to use random points, like Vornoi and Spacial, those points won't be in the same places. " +
            "They are re-generated upon loading the preset.\n"
            , EditorStyles.wordWrappedLabel);
    }

    private void OnGUI()
    {
        EditorStyles.label.normal.textColor = Color.white;
        EditorStyles.label.hover.textColor = Color.cyan;
        EditorStyles.foldout.normal.textColor = Color.white;
        EditorStyles.foldout.hover.textColor = Color.cyan;

        EditorStyles.wordWrappedLabel.normal.textColor = Color.white;
        EditorStyles.centeredGreyMiniLabel.normal.textColor = Color.white;


        handleMouse();


        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, grayBack);
        EditorGUILayout.BeginHorizontal(); // the generate/edit/information row

        if (GUILayout.Button("Generate"))
        {
            mode = 0;
            if (selectedInfo != null)
            {
                selectedInfo.updateImage();
            }
        }

        if (GUILayout.Button("Edit"))
        {
            mode = 1;
            if (selectedEdit != null)
            {
                selectedEdit.updateImage();
            }
        }

        GUI.backgroundColor = Color.magenta;
        if (GUILayout.Button("ⓘ", GUILayout.Width(24)))
        {
            information = !information;
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();



        if (information) // show the information panel
        {
            informationGenerate();
            EditorGUILayout.EndScrollView();
            return;
        }


        // click the drag n' drop area to select the save/output folder
        if (GUILayout.Button("", EditorStyles.boldLabel, GUILayout.Height(50)))
        {
            if (saveLocation != "")
            {
                
                if (Directory.Exists(Application.dataPath + saveLocation))
                {
                    UnityEngine.Object obj = (DefaultAsset)AssetDatabase.LoadAssetAtPath("Assets/" + saveLocation.Substring(0,saveLocation.Length-1), typeof(DefaultAsset));

                    Selection.activeObject = obj;

                    EditorGUIUtility.PingObject(obj);
                }
            }else
            {
                if (Directory.Exists(defaulSaveLocation()))
                {

                    UnityEngine.Object obj = (DefaultAsset)AssetDatabase.LoadAssetAtPath("Assets/UniNoise/Saves", typeof(DefaultAsset));


                    Selection.activeObject = obj;

                    EditorGUIUtility.PingObject(obj);
                }
            }
        }
        DropAreaGUI(GUILayoutUtility.GetLastRect()); // do the drag n' drop save location


        // handles generate and edit tabs, the preview, and the selected type's variables
        mainGUI();




        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// handles generate and edit tabs, the preview, and the selected type's variables
    /// </summary>
    void mainGUI()
    {
        if (mode == 0) // generate tab
        {
            // show the generation type buttons
            generateTab();
            if (selectedInfo != null)
            {
                // get the selected type's label
                EditorGUILayout.LabelField(selectedInfo.getTextureName() + " Settings", EditorStyles.centeredGreyMiniLabel);
                
                // show the selected type's GUI/variabels
                selectedInfo.doGUI();
            }
        }
        else if (mode == 1) // edit tab
        {
            // show the edit type buttons
            editTab();
            if (selectedEdit != null)
            {
                EditorGUILayout.LabelField(selectedEdit.getTextureName() + " Settings", EditorStyles.centeredGreyMiniLabel);
                selectedEdit.doGUI();
            }
        }
    }

    /// <summary>
    /// Track if mouse 0 is held
    /// </summary>
    void handleMouse()
    {
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            Repaint();
            mouseDown = true;
        }else if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
        {
            Repaint();
            mouseDown = false;
        }
    }

    /// <summary>
    /// Show the selectable Generate tab type buttons, texture size, and gradients
    /// </summary>
    public void generateTab()
    {
        textureSize = EditorGUILayout.IntField("Texture Size : ", textureSize);

        // click to apply random gradients
        gradientsAB.target = EditorGUILayout.Foldout(gradientsAB.target, "Random Gradients");
        if (EditorGUILayout.BeginFadeGroup(gradientsAB.faded))
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("2 Color"))
            {
                Color2Gradient();
            }

            if (GUILayout.Button("Random"))
            {
                colorRandomGradient();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndFadeGroup();


        typeAB.target = EditorGUILayout.Foldout(typeAB.target, "Pattern Types");
        if (EditorGUILayout.BeginFadeGroup(typeAB.faded))
        {
            // all of the generate buttons for each type of generation


            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();

            if (typeColorButton("Perlin"))
            {
                selectedInfo = new newPerlinSettings(this);
            }

            if (typeColorButton("DGN"))
            {
                selectedInfo = new DGN(this);
            }

            if (typeColorButton("Voronoi"))
            {
                selectedInfo = new vornoiSettings(this, false);
            }

            if (typeColorButton("Noise"))
            {
                selectedInfo = new noiseSettings(this);
            }

            if (typeColorButton("MarchSquares"))
            {
                selectedInfo = new marchSettings(this);
            }

            if (typeColorButton("MarchCircles"))
            {
                selectedInfo = new marchCircleSettings(this);
            }

            if (typeColorButton("Spacial"))
            {
                selectedInfo = new spacialNoiseSettings(this);
            }

            if (typeColorButton("Truchet"))
            {
                selectedInfo = new newTruchet(this);
            }

            if (typeColorButton("Houndstooth"))
            {
                selectedInfo = new houndstoothSettings(this);
            }

            if (typeColorButton("Scales"))
            {
                selectedInfo = new scalesSettings(this);
            }

            if (typeColorButton("Strokes"))
            {
                selectedInfo = new strokesSettings(this);
            }


            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();



            if (typeColorButton("Normal Boxes"))
            {
                selectedInfo = new BoxNoiseSettings(this);
            }

            if (typeColorButton("Stripes"))
            {
                selectedInfo = new stripeSettings(this);
            }

            if (typeColorButton("Checkers"))
            {
                selectedInfo = new checkerSettings(this);
            }

            if (typeColorButton("Waves"))
            {
                selectedInfo = new WaveSettings(this);
            }

            if (typeColorButton("Grid"))
            {
                selectedInfo = new gridSettings(this);
            }

            if (typeColorButton("Triangles"))
            {
                selectedInfo = new TriGridSettings(this);
            }

            if (typeColorButton("Gradient"))
            {
                selectedInfo = new gradientSettings(this);
            }

            if (typeColorButton("Cubes"))
            {
                selectedInfo = new CubeSettings(this);
            }

            if (typeColorButton("Plaid"))
            {
                selectedInfo = new plaidSettings(this);
            }

            if (typeColorButton("Dots"))
            {
                selectedInfo = new dotsSettings(this);
            }

            if (typeColorButton("Channel2Channel"))
            {
                selectedInfo = new channel2Channel(this);
            }

            resetTypeColorButtons();

            //
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();


        }
        EditorGUILayout.EndFadeGroup();

        EditorGUILayout.Separator();



        if (previewTex)
        {
            drawPreviewTextureGUI();
        }
        saveButton();
        if (previewTex)
        {
            EditorGUI.BeginChangeCheck();
            mainGradient = EditorGUILayout.GradientField(mainGradient);
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedInfo != null)
                {
                    selectedInfo.updateImage();
                }
            }

        }
    }

    /// <summary>
    /// Show the selectable Edit tab type buttons
    /// </summary>
    public void editTab()
    {
        EditorGUI.BeginChangeCheck();
        texToEdit = EditorGUILayout.ObjectField("Texture to Edit", texToEdit, typeof(Texture2D), false) as Texture2D;
        if (!texToEdit)
        {
            EditorGUILayout.HelpBox("Select a texture to modify.", MessageType.Info);
        }
        if (EditorGUI.EndChangeCheck())
        {
            if (texToEdit && selectedEdit != null)
            {
                selectedEdit.setInputTex(texToEdit);
                selectedEdit.updateImage();
            }
        }

        if (!texToEdit)
        {
            return;
        }

        

        if (typeColorButtonEdit("Blur"))
        {
            selectedEdit = new BlurSettings(this, texToEdit);
        }

        if (typeColorButtonEdit("Normal"))
        {
            selectedEdit = new NormalSettings(this, texToEdit);
        }

        if (typeColorButtonEdit("Color Ramp"))
        {
            selectedEdit = new ColorRampSettings(this, texToEdit);
        }

        if (typeColorButtonEdit("Colorize"))
        {
            selectedEdit = new ColorizeSettings(this, texToEdit);
        }

        if (typeColorButtonEdit("Warp"))
        {
            selectedEdit = new warpSettings(this, texToEdit);
        }

        if (typeColorButtonEdit("Blend"))
        {
            selectedEdit = new BlendSettings(this, texToEdit);
        }

        if (typeColorButtonEdit("Outline"))
        {
            selectedEdit = new OutlineSettings(this, texToEdit);
        }

        if (typeColorButtonEdit("Particleize"))
        {
            selectedEdit = new ParticleizeSettings(this, texToEdit);
        }

        resetTypeColorButtons();

        if (previewTex)
        {
            drawPreviewTextureGUI();
        }

        saveButton();
    }

    // No idea why I went with this... These are used for which buttons are selected or to be highlighted
    // I had to be tired writing a lot of this...
    static int selectedInfoType = 0;
    static int scanningInfoType = 0;
    static int selectedEditType = 0;
    void resetTypeColorButtons()
    {
        scanningInfoType = 0;
    }


    /// <summary>
    /// Shows a generate button and selects it for highlighting.
    /// The string should be the same as the name in the GenOrEditClass of this type.
    /// </summary>
    /// <param name="name"> </param>
    /// <returns>True if the button is clicked</returns>
    bool typeColorButton(string name)
    {
        if (scanningInfoType == selectedInfoType)
        {
            GUI.backgroundColor = Color.cyan;
        }

        bool val = GUILayout.Button(name);

        if (val)
        {
            selectedInfoType = scanningInfoType;
        }

        GUI.backgroundColor = Color.white;
        scanningInfoType++;
        return val;
    }

    /// <summary>
    /// Same thing as the above method....... but for the Edit panel 
    /// </summary>
    bool typeColorButtonEdit(string name)
    {
        if (scanningInfoType == selectedEditType)
        {
            GUI.backgroundColor = Color.cyan;
        }

        bool val = GUILayout.Button(name);

        if (val)
        {
            selectedEditType = scanningInfoType;
        }

        GUI.backgroundColor = Color.white;
        scanningInfoType++;
        return val;
    }

 
    /// <summary>
    /// return the current save location selected/defaulted for the image output
    /// </summary>
    /// <returns></returns>
    string getSaveLocation()
    {
        return saveLocation == "" ? defaulSaveLocation() : Application.dataPath + saveLocation;
    }

    /// <summary>
    /// return the default save location for the image output
    /// </summary>
    /// <returns></returns>
    static string defaulSaveLocation()
    {
        return Application.dataPath + "/UniNoise/Saves/";
    }

    /// <summary>
    /// Save the final preview texture
    /// </summary>
    void saveButton()
    {
        if (previewTex == null)
        {
            return;
        }

        GUI.color = Color.green;
        if (GUILayout.Button("Save"))
        {
            if (previewTex)
            {
                if (selectedEdit != null && mode == 1) // for shaders that have a different preview than output
                {
                    if (selectedEdit.hasPreviewImageUpdate)
                    {
                        selectedEdit.updateImageFinal();
                    }
                }
                
                // basic saving goodness
                string S = getSaveLocation() + getFilename() + ".png";
                Debug.Log("Saved to : " + S);
                File.WriteAllBytes(S, previewTex.EncodeToPNG());
                
                string relativepath = "Assets" + S.Substring(Application.dataPath.Length);
                AssetDatabase.ImportAsset(relativepath);
                TextureImporter Ti = (TextureImporter)AssetImporter.GetAtPath(relativepath);
                Ti.wrapMode = TextureWrapMode.Repeat;
                Ti.SaveAndReimport();

                if (selectedEdit != null && mode == 1) // for shaders that have a different preview than output
                {
                    if (selectedEdit.hasPreviewImageUpdate)
                    {
                        selectedEdit.updateImage();
                    }
                }
            }
        }
        GUI.color = Color.white;
    }

    /// <summary>
    /// Draws the main preview in the editor window, includes the popup preview button
    /// </summary>
    void drawPreviewTextureGUI()
    {
        if ((mode == 0 && selectedInfo == null) || (mode == 1 && selectedEdit == null))
        {
            return;
        }

        // show the non-popup preview texture, and the option for the popup preview
        if (previewTex && UniNoiseTexturePreview.main == null)
        {
            if (GUILayout.Button("Popout view"))
            {
                if (UniNoiseTexturePreview.main == null)
                {
                    UniNoiseTexturePreview.ShowWindow();
                }
                else
                {
                    UniNoiseTexturePreview.main.Close();
                }
            }

            // draw the preview texture, tiled horizontally
            Rect LR = GUILayoutUtility.GetLastRect();
            LR.min += new Vector2(((position.width) - 256) / 2f, LR.height + 10);
            LR.size = new Vector2(256, 256);
            Rect holdLR = new Rect(LR.position - new Vector2(256,0), LR.size + new Vector2((256 * 2f),0));
            EditorGUI.DrawPreviewTexture(LR, previewTex);
            LR.position += new Vector2(-256, 0);
            EditorGUI.DrawPreviewTexture(LR, previewTex);
            LR.position += new Vector2(512, 0);
            EditorGUI.DrawPreviewTexture(LR, previewTex);
            GUILayout.Space(256 + 22);

            // drawa a rectangle around the center image
            GL.Flush();
            GUI.BeginClip(LR);
            GL.Clear(true, false, Color.black);
            if (glMat)
            {
                glMat.SetPass(0);
            }
            GL.Begin(GL.LINES);
            GL.Color(Color.cyan); // cyan is the best
            GL.Vertex(new Vector3(0,0,0));
            GL.Vertex(new Vector3(-LR.width,0,0));
            GL.Vertex(new Vector3(-LR.width, 0, 0));
            GL.Vertex(new Vector3(-LR.width, LR.height, 0));
            GL.Vertex(new Vector3(-LR.width, LR.height, 0));
            GL.Vertex(new Vector3(0, LR.height, 0));
            GL.Vertex(new Vector3(0, LR.height, 0));
            GL.Vertex(new Vector3(0, 0, 0));    
            GL.End();
            GUI.EndClip();

            // zoom preview when clicking on the preview
            if (mouseDown && holdLR.Contains(Event.current.mousePosition))
            {
                Repaint();
                Vector2 size = new Vector2(200, 200);
                Vector2 position = Event.current.mousePosition - (size/2f);
                Rect R = new Rect(position, size);

                Vector2 localPosition = Event.current.mousePosition - holdLR.position;

                Rect TC = new Rect(new Vector2((localPosition.x/ 256f)-.125f, (1-(localPosition.y / 256f))-.125f), new Vector2(.25f,.25f));

                EditorGUI.DrawPreviewTexture(R, previewTex);
                GUI.DrawTextureWithTexCoords(R, previewTex, TC, false);
            }
        }
    }

    
    /// <summary>
    /// Get a useable filename to save with
    /// </summary>
    /// <returns></returns>
    string getFilename()
    {
        string path = getSaveLocation();

        string[] files = Directory.GetFiles(path);

        int count = 0; 
        
        string FName = getTextureName();

        List<string> names = new List<string>();
        foreach (string S in files)
        {
            string N = Path.GetFileName(S);
            string FT = Path.GetExtension(S);
            if (N.Contains(FName) && (N.Substring(N.Length - 5, 5) != (".meta")))
            {
                names.Add(N);
                count++;
            }
        }

        string finalName = FName + count.ToString();
        int attempts = 0;

        while (names.Contains(finalName+".png") && attempts < 500)
        {
            attempts++;
            count++;
            finalName = FName + count.ToString();
        }



        return finalName;
    }

    /// <summary>
    /// Get a name for the selected type to save with
    /// </summary>
    /// <returns></returns>
    string getTextureName()
    {
        
        if (Main.mode == 0)
        {
            if (selectedInfo == null)
            {
                return "Unknown";
            }
            return selectedInfo.getTextureName();
        }else if (Main.mode == 1)
        {
            if (selectedEdit == null)
            {
                return "Unknown";
            }
            return selectedEdit.getTextureName();
        }
        return selectedInfo.getTextureName();
    }

    /// <summary>
    /// Generate some 0 - 1 Vector3s that tile -1 - 2
    /// </summary>
    /// <param name="pointCount"></param>
    /// <param name="flat"></param>
    /// <param name="is3d"></param>
    /// <param name="flip"></param>
    /// <returns></returns>
    public List<Vector3> seamlessPoints(int pointCount, bool flat = false, bool is3d = false, bool flip = false)
    {
        List<Vector3> points = new List<Vector3>();
        List<Vector3> originalPoints = new List<Vector3>();
        for (int i = 0; i < pointCount; i++)
        {
            originalPoints.Add(new Vector3(Random.Range(0f, 1f), flat ? 0 : Random.Range(0f, 1f), Random.Range(0f, 1f)));
        }

        if (flip)
        {
            for (int i = 0; i < originalPoints.Count; i++)
            {
                Vector3 flipx = new Vector3(1 - originalPoints[i].x, originalPoints[i].y, originalPoints[i].z);
                Vector3 flipz = new Vector3(originalPoints[i].x, originalPoints[i].y, 1-originalPoints[i].z);
                Vector3 flipxz = new Vector3(1-originalPoints[i].x, originalPoints[i].y, 1 - originalPoints[i].z);
                points.Add(originalPoints[i]);
                points.Add(flipx);
                points.Add(flipz);
                points.Add(flipxz);
            }
            return points;
        }
        for (int i = 0; i < pointCount; i++)
        {
            points.Add(originalPoints[i]);
            points.Add(originalPoints[i] + new Vector3(-1, 0, 0));
            points.Add(originalPoints[i] + new Vector3(-1, 0, -1));
            points.Add(originalPoints[i] + new Vector3(0, 0, -1));
            points.Add(originalPoints[i] + new Vector3(1, 0, -1));
            points.Add(originalPoints[i] + new Vector3(1, 0, 0));
            points.Add(originalPoints[i] + new Vector3(1, 0, 1));
            points.Add(originalPoints[i] + new Vector3(0, 0, 1));
            points.Add(originalPoints[i] + new Vector3(-1, 0, 1));
        }

        if (is3d)
        {
            for (int i = 0; i < pointCount; i++)
            {
                points.Add(originalPoints[i] + new Vector3(-1, 1, 0));
                points.Add(originalPoints[i] + new Vector3(-1, 1, -1));
                points.Add(originalPoints[i] + new Vector3(0, 1, -1));
                points.Add(originalPoints[i] + new Vector3(1, 1, -1));
                points.Add(originalPoints[i] + new Vector3(1, 1, 0));
                points.Add(originalPoints[i] + new Vector3(1, 1, 1));
                points.Add(originalPoints[i] + new Vector3(0, 1, 1));
                points.Add(originalPoints[i] + new Vector3(-1, 1, 1));

                points.Add(originalPoints[i] + new Vector3(-1, -1, 0));
                points.Add(originalPoints[i] + new Vector3(-1, -1, -1));
                points.Add(originalPoints[i] + new Vector3(0, -1, -1));
                points.Add(originalPoints[i] + new Vector3(1, -1, -1));
                points.Add(originalPoints[i] + new Vector3(1, -1, 0));
                points.Add(originalPoints[i] + new Vector3(1, -1, 1));
                points.Add(originalPoints[i] + new Vector3(0, -1, 1));
                points.Add(originalPoints[i] + new Vector3(-1, -1, 1));
            }
        }


        return points;
    }

    /// <summary>
    /// Get a clean slate, using the selected output size
    /// </summary>
    /// <returns></returns>
    public Texture2D blankTex()
    {
        Texture2D T = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        T.filterMode = FilterMode.Bilinear;
        return T;
    }

    /// <summary>
    /// Last gradient texture generated, used in shaders to apply gradients to the generations
    /// </summary>
    public static Texture2D lastGradientTex;
    public Texture2D gradientTexture()
    {
        if (lastGradientTex)
        {
            DestroyImmediate(lastGradientTex);
        }
        Texture2D gradientTex = new Texture2D(64, 1);
        gradientTex.wrapMode = TextureWrapMode.Clamp;
        
        for (int i = 0; i < 64; i++)
        {
            gradientTex.SetPixel(i, 0, mainGradient.Evaluate((float)i / 64f));
        }
        gradientTex.Apply();
        lastGradientTex = gradientTex;
        return gradientTex;
    }

    public void setInfoDirty()
    {
        if (info != null)
        {
            EditorUtility.SetDirty(info);
        }
    }

    /// <summary>
    /// Called from the GenOrEditClass... class... to destroy... things
    /// </summary>
    /// <param name="O"></param>
    public void destroyObj(UnityEngine.Object O)
    {
        DestroyImmediate(O);
    }
}


public class GenOrEditClass
{
    public Texture2D T;
    public bool hasPreviewImageUpdate = false;
    /// <summary>
    /// Get name for saving
    /// </summary>
    public virtual string getTextureName() { return "None"; }
    /// <summary>
    /// Show the GUI for this type
    /// </summary>
    public virtual void doGUI() {}
    /// <summary>
    /// Do the generation
    /// </summary>
    public virtual void updateImage() { }
    /// <summary>
    /// For images who's output is different than the preview (some generations fake alpha with a grid under the image)
    /// </summary>
    public virtual void updateImageFinal() { }
    /// <summary>
    /// Sets a materials main texture, for the edit panel
    /// </summary>
    public virtual void setInputTex(Texture2D Tex) { }

    UniNoiseScriptableInfo.saveablePreset[] possiblePresets; // list of presets for the seleted type

    public static UnityEditor.AnimatedValues.AnimBool presetsAB; // for animating dropdowns

    /// <summary>
    /// Setup the dropdown animation and grab the applicable presets
    /// </summary>
    public GenOrEditClass() // 
    {
        if (presetsAB == null)
        {
            presetsAB = new UnityEditor.AnimatedValues.AnimBool(false);
            presetsAB.valueChanged.AddListener(UniNoiseWindow.Main.Repaint);
        }
        possiblePresets = getPresets();
    }

    /// <summary>
    /// Returns the applicable presets for this generation type, using the type name
    /// </summary>
    /// <returns></returns>
    UniNoiseScriptableInfo.saveablePreset[] getPresets()
    {
        List<UniNoiseScriptableInfo.saveablePreset> holds = new List<UniNoiseScriptableInfo.saveablePreset>();
        foreach(UniNoiseScriptableInfo.saveablePreset I in UniNoiseWindow.info.presets)
        {
            if (I.genOrEditTypeName == getTextureName())
            {
                holds.Add(I);
            }
        }
        return holds.ToArray();

    }

    /// <summary>
    /// Apply a saved preset.
    /// </summary>
    /// <param name="P"></param>
    public void loadPreset(UniNoiseScriptableInfo.saveablePreset P)
    {
        System.Reflection.FieldInfo[] fields = this.GetType().GetFields();
        if (P.gradient != null)
        {
            Gradient grabGradient = P.gradient.getGradient();
            UniNoiseWindow.Main.mainGradient.alphaKeys = grabGradient.alphaKeys;
            UniNoiseWindow.Main.mainGradient.colorKeys = grabGradient.colorKeys;
            UniNoiseWindow.Main.mainGradient.mode = grabGradient.mode;
        }
        foreach (UniNoiseScriptableInfo.presetVariable p in P.variables)
        {
            foreach(System.Reflection.FieldInfo f in fields)
            {
                if (f.Name == p.variableName)
                {
                    setVariable(f, p);
                }
            }
        }
        updateImage();
    }

    /// <summary>
    /// Sets a variable from a preset
    /// </summary>
    /// <param name="f"></param>
    /// <param name="p"></param>
    void setVariable(System.Reflection.FieldInfo f, UniNoiseScriptableInfo.presetVariable p)
    {
        switch (p.variableType)
        {
            case "System.Boolean": f.SetValue(this, p.variableValue == "True" ? true : false); break;
            case "System.Int32": f.SetValue(this, int.Parse(p.variableValue)); break;
            case "System.Single": f.SetValue(this, float.Parse(p.variableValue)); break;
        }
    }

    /// <summary>
    /// Save the current setup to a preset
    /// </summary>
    /// <param name="toReplace"></param>
    public void savePreset(UniNoiseScriptableInfo.saveablePreset toReplace = null)
    {

        UniNoiseScriptableInfo.saveablePreset S = new UniNoiseScriptableInfo.saveablePreset();
        S.genOrEditTypeName = getTextureName();
        S.name = S.genOrEditTypeName;
        if (toReplace != null)
        {
            toReplace.variables = new List<UniNoiseScriptableInfo.presetVariable>();
            S = toReplace;
        }
        
        System.Type T = GetType();
        System.Reflection.FieldInfo[] fields = T.GetFields();

        S.gradient = new UniNoiseScriptableInfo.saveableGradient(UniNoiseWindow.Main.mainGradient);
        foreach (System.Reflection.FieldInfo I in fields)
        {
            UniNoiseScriptableInfo.presetVariable N = new UniNoiseScriptableInfo.presetVariable(I.Name, I.GetValue(this).ToString(), I.FieldType.ToString());
            S.variables.Add(N);
        }
        if (UniNoiseWindow.info != null && toReplace == null)
        {
            UniNoiseWindow.info.presets.Add(S);
        }

        
    }

    /// <summary>
    /// Button used to select different versions in a material, returns true if the button is clicked.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="isSelected"></param>
    /// <returns></returns>
    public bool typeButton(string name, bool isSelected)
    {
        GUI.color = isSelected ? Color.cyan : Color.white;
        bool selected = GUILayout.Button(name);
        GUI.color = Color.white;
        return (selected);
    }


    RenderTexture RT;
    /// <summary>
    /// Renders the generation and sets it as the preview texture
    /// </summary>
    /// <param name="myWindow"></param>
    /// <param name="myMat"></param>
    public void applyGeneration(UniNoiseWindow myWindow, Material myMat)
    {
        
        if (T == null || T.width != myWindow.textureSize || T.height != myWindow.textureSize)
        {
            if (T != null)
            {
                myWindow.destroyObj(T);
            }
            T = myWindow.blankTex();

            if (RT != null)
            {
                RT.Release();
            }
            RT = new RenderTexture(T.width, T.height, 0);
        }
        if (RT == null)
        {
            RT = new RenderTexture(T.width, T.height, 0);
        }

        Graphics.Blit(T, RT, myMat);

        RenderTexture.active = RT;
        Texture2D blank = myWindow.blankTex();
        blank.ReadPixels(new Rect(0, 0, T.width, T.height), 0, 0);

        blank.Apply();
        myWindow.setPreviewTexture(blank);
    }

    /// <summary>
    /// Show the preset menu, returns true if the reset button is clicked.... yuck
    /// </summary>
    /// <returns></returns>
    public bool genericMenuReturnsReset()
    {
        GUI.color = Color.yellow;
        bool R = GUILayout.Button("Reset");
        GUI.color = Color.white;
        
        presetsAB.target = EditorGUILayout.Foldout(presetsAB.target, "Presets");
        if (EditorGUILayout.BeginFadeGroup(presetsAB.faded))
        {
            if (GUILayout.Button("Save Preset"))
            {
                savePreset();
                possiblePresets = getPresets();
                UniNoiseWindow.Main.setInfoDirty();
            }
            for (int i = 0; i < possiblePresets.Length; i++)
            {
                UniNoiseScriptableInfo.saveablePreset P = possiblePresets[i];
                EditorGUILayout.BeginHorizontal();
                GUI.color = Color.green;
                if (GUILayout.Button("Load", GUILayout.Width(80)))
                {
                    loadPreset(P);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                GUI.color = Color.white;
                P.name = EditorGUILayout.TextField(P.name);

                GUI.color = Color.yellow;
                if (GUILayout.Button("Replace", GUILayout.Width(100)))
                {
                    savePreset(P);
                    EditorGUILayout.EndHorizontal();
                    UniNoiseWindow.Main.setInfoDirty();
                    break;
                }
                GUI.color = Color.red;
                if (GUILayout.Button("X", GUILayout.Width(40))){
                    UniNoiseWindow.info.presets.Remove(P);
                    possiblePresets = getPresets();
                    EditorGUILayout.EndHorizontal();
                    UniNoiseWindow.Main.setInfoDirty();
                    break;
                }
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }


        }
        EditorGUILayout.EndFadeGroup();
            return R;
    }
}



/// BELOW THIS IS JUST SUBCLASSES OF GenOrEditClass
/// Each type of generation/edit has their own class for GUI, material control, and generating


// this one is commented to help you understand the madness, if you want to add more buttons. Buttons are good.
public class newPerlinSettings : GenOrEditClass
{

    /// <summary>
    /// I could have just made this a variable, but I didn't... I'm sure I had my reasons....
    /// Used for saving and the text in the button to select this type.
    /// </summary>
    /// <returns></returns>
    public override string getTextureName()
    {
        return "Perlin";
    }


    // All the subclasses need the myWindow and myMat variable.
    // I definitely couldn't just have this in the parent class because... reasons.
    UniNoiseWindow myWindow;
    Material myMat;

    // All the variables that are used for the shader.
    public int octaves = 10;
    public float scalex = 3f;
    public float scaley = 3f;
    public int type = 0;
    public float power = 1;
    public float seed = 0.00372f;
    public float step = .39f;
    public float contrast = 0;
    public float lacunarity = 3;
    public float weight = .62f;
    public bool turbulent = false;
    public float gain = 0;

    // find the shader and do the first preview render
    public newPerlinSettings(UniNoiseWindow window)
    {
        myWindow = window;

        myMat = new Material(Shader.Find("Hidden/NewPerlin"));

        updateImage();
    }

    // reset to defaults.
    void reset()
    {
        myWindow.selectedInfo = new newPerlinSettings(myWindow);
    }

    // set the material's values and render the preview
    public override void updateImage()
    {
        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myWindow.blitmat = myMat;
        myMat.SetFloat("_ScaleX", scalex);
        myMat.SetFloat("_ScaleY", scaley);
        myMat.SetInt("_Type", type);
        myMat.SetInt("_Octaves", octaves);
        myMat.SetFloat("_Power", power);
        myMat.SetFloat("seed", seed);
        myMat.SetFloat("step", step);
        myMat.SetFloat("contrast", contrast);
        myMat.SetFloat("lacunarity", lacunarity);
        myMat.SetFloat("weight", weight);
        myMat.SetInt("turbulent", turbulent ?1:0);
        myMat.SetFloat("gain", gain);
        base.applyGeneration(myWindow, myMat);
    }

    // set all of the variables setup above with GUI goodness
    public override void doGUI()
    {
        // only use this if you want this type to have presets
        if (base.genericMenuReturnsReset())
        {
            reset();
        }

        EditorGUI.BeginChangeCheck();

        // GUI MAYHEM vvvv

        seed = EditorGUILayout.Slider("Seed", seed, 0f, .01f);
        octaves = (int)EditorGUILayout.Slider("Octaves", octaves, 1, 20);
        scalex = EditorGUILayout.Slider("ScaleX", scalex, 1, 50);
        scaley = EditorGUILayout.Slider("ScaleY", scaley, 1, 50);
        turbulent = EditorGUILayout.Toggle("Turbulent", turbulent);
        GUI.color = octaves == 1 ? Color.red : Color.white;
        lacunarity = (int)EditorGUILayout.Slider("Detail", lacunarity, 2,7);
        weight = EditorGUILayout.Slider("Detail Weight", weight, .2f, .9f);
        GUI.color = Color.white;
        step = EditorGUILayout.Slider("Step", step, -2f, 2f);

        contrast = EditorGUILayout.Slider("Contrast", contrast, -1f, 1f);
        gain = EditorGUILayout.Slider("Gain", gain, -1f, 1f);

        power = EditorGUILayout.Slider("Warp Power", power, 0f, 10f);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        if (typeButton("Perlin", type == 0)) {
            type = 0;
        }
        if (typeButton("Lines", type == 1))
        {
            type = 1;
        }
        if (typeButton("Dots", type == 2))
        {
            type = 2;
        }
        if (typeButton("Squares", type == 3))
        {
            type = 3;
        }
        if (typeButton("Warped", type == 8))
        {
            type = 8;
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical();
        if (typeButton("Grid", type == 4))
        {
            type = 4;
        }
        if (typeButton("Tangent", type == 5))
        {
            type = 5;
        }
        if (typeButton("Sined", type == 6))
        {
            type = 6;
        }
        if (typeButton("Sawed", type == 7))
        {
            type = 7;
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        
        // Render the preview if the mayhem has changed.
        if ( EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}


public class channel2Channel : GenOrEditClass
{

    public override string getTextureName()
    {
        return "Channel2Channel";
    }


    UniNoiseWindow myWindow;
    Material myMat;

    public Texture2D redChannel;
    public Texture2D greenChannel;
    public Texture2D blueChannel;

    public bool rAlpha;
    public bool gAlpha;
    public bool bAlpha;

    // find the shader and do the first preview render
    public channel2Channel(UniNoiseWindow window)
    {
        myWindow = window;

        myMat = new Material(Shader.Find("Hidden/UniNoiseChannel2Channel"));

        updateImage();
    }

    // reset to defaults.
    void reset()
    {
        myWindow.selectedInfo = new channel2Channel(myWindow);
    }

    // set the material's values and render the preview
    public override void updateImage()
    {
        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myMat.SetTexture("_TexR", redChannel);
        myMat.SetInt("_RAlpha", rAlpha ? 1 : 0);
        myMat.SetTexture("_TexG", greenChannel);
        myMat.SetInt("_GAlpha", gAlpha ? 1 : 0);
        myMat.SetTexture("_TexB", blueChannel);
        myMat.SetInt("_BAlpha", bAlpha ? 1 : 0);
        //
        myWindow.blitmat = myMat;
        base.applyGeneration(myWindow, myMat);
    }

    // set all of the variables setup above with GUI goodness
    public override void doGUI()
    {
        //if (base.genericMenuReturnsReset())
        //{
        //    reset();
        //}

        EditorGUI.BeginChangeCheck();

        // GUI MAYHEM vvvv

        rAlpha = EditorGUILayout.Toggle("Use Red Alpha", rAlpha);
        redChannel = EditorGUILayout.ObjectField("Red Channel", redChannel, typeof(Texture2D), false) as Texture2D;
        gAlpha = EditorGUILayout.Toggle("Use Green Alpha", gAlpha);
        greenChannel = EditorGUILayout.ObjectField("Green Channel", greenChannel, typeof(Texture2D), false) as Texture2D;
        bAlpha = EditorGUILayout.Toggle("Use Blue Alpha", bAlpha);
        blueChannel = EditorGUILayout.ObjectField("Blue Channel", blueChannel, typeof(Texture2D), false) as Texture2D;

        // Render the preview if the mayhem has changed.
        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}


public class vornoiSettings : GenOrEditClass
{
    
    public override string getTextureName()
    {
        return "Vornoi " + (second ? "1" : "2");
    }
    public int pointCount = 5;
    UniNoiseWindow myWindow;
    public bool second = false;
    Material myMat;
    Vector4[] myPoints;
    public int lastPointCount = 5;
    public bool hasPoints = false;
    public float yPosition = 0;
    public float yMultiplier = 1;
    public int type = 0;

    public float warpX = 0;
    public float warpY = 0;
    public int octaves = 1;
    public float contrast = 1;
    public float gain = 0;
    public float octaveWeight = .95f;

    void reset()
    {
        myWindow.selectedInfo = new vornoiSettings(myWindow, false);
    }

    public vornoiSettings(UniNoiseWindow window, bool isSecond)
    {
        
        second = isSecond;
        myWindow = window;
        pointCount = 10;
        updateImage();
    }

    public Vector4[] pointPositions(int size)
    {
        if (myMat)
        {
            myWindow.destroyObj(myMat);
        }
        myMat = new Material(Shader.Find("Hidden/VornoiShader"));

        List<Vector4> points = new List<Vector4>();
        for (int i = 0; i < size*5; i++)
        {
            points.Add(new Vector4(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
        }
        return points.ToArray();
    }


    public override void updateImage()
    {
        if (!hasPoints || lastPointCount != pointCount)
        {
            myPoints = pointPositions(pointCount);
            hasPoints = true;
            lastPointCount = pointCount;
        }

        
        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myWindow.blitmat = myMat;
        myMat.SetInt("_VectorCount", pointCount);
        myMat.SetVectorArray("_points", myPoints);
        myMat.SetFloat("yPosition", yPosition);
        myMat.SetFloat("warpX", warpX);
        myMat.SetFloat("warpY", warpY);
        myMat.SetFloat("yMultiplier", yMultiplier);
        myMat.SetInt("_Type", type);
        myMat.SetInt("octaves", octaves);
        myMat.SetFloat("contrast", contrast);
        myMat.SetFloat("gain", gain);
        myMat.SetFloat("octaveWeight", octaveWeight);


        base.applyGeneration(myWindow, myMat);
    }

    

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        pointCount = (int)EditorGUILayout.Slider("Points", pointCount, 1, 100);
        octaves = (int)EditorGUILayout.Slider("Octaves", octaves, 1, 8);
        octaveWeight = EditorGUILayout.Slider("Octave Weight", octaveWeight, 0f, 1f);
        yPosition = EditorGUILayout.Slider("Y Position", yPosition, 0f, 1f);
        yMultiplier = EditorGUILayout.Slider("Y Multiplier", yMultiplier, 0f, 1f);
        warpX = EditorGUILayout.Slider("Warp X", warpX, -1f, 1f);
        warpY = EditorGUILayout.Slider("Warp Y", warpY, -1f, 1f);
        contrast = EditorGUILayout.Slider("Contrast", contrast, 0f, 5f);
        if (type == 5 || type == 6)
        {
            GUI.color = Color.green;
        }
        gain = EditorGUILayout.Slider("Gain", gain, -2f, 2f);
        GUI.color = Color.white;


        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        if (typeButton("Nearest", type == 0))
        {
            type = 0;
        }
        if (typeButton("Hue", type == 1))
        {
            type = 1;
        }
        if (typeButton("F2/F1", type == 5))
        {
            type = 5;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical();
        if (typeButton("Second", type == 3))
        {
            type = 3;
        }
        if (typeButton("Second Hue", type == 4))
        {
            type = 4;
        }
        if (typeButton("Solid Hue", type == 2))
        {
            type = 2;
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class strokesSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Strokes";
    }
    public int scale = 10;
    public float layerWeight = 0f;
    public float detailSize = 1f;
    public float power = .035f;
    public float scratchCount = 1f;
    public float brightness = 1;
    public float contrast = 1f;
    public float squiggles = 1;
    public int octaves = 1;
    public float testFloat = 1;
    public float sides = 0;
    public float seed = 0;
    int type = 0;
    UniNoiseWindow myWindow;
    Material myMat;

    void reset()
    {
        myWindow.selectedInfo = new strokesSettings(myWindow);
    }

    public strokesSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseStrokes"));
        myWindow = window;
        updateImage();
    }


    public override void updateImage()
    {

        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myWindow.blitmat = myMat;
        myMat.SetFloat("seed", seed);
        myMat.SetFloat("scale", scale);
        myMat.SetFloat("layerWeight", layerWeight);
        myMat.SetFloat("detailSize", detailSize);
        myMat.SetFloat("power", power);
        myMat.SetFloat("scratchCount", scratchCount);
        myMat.SetFloat("brightness", brightness);
        myMat.SetFloat("squiggles", squiggles);
        myMat.SetInt("octaves", octaves);
        myMat.SetFloat("testFloat", testFloat);
        myMat.SetFloat("contrast", contrast);
        myMat.SetInt("type", type);
        myMat.SetFloat("sides", sides);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        seed = EditorGUILayout.Slider("Seed", seed, 0f, 10f);
        scale = (int)EditorGUILayout.Slider("Scale", scale, 1, 50);
        octaves = (int)EditorGUILayout.Slider("Octaves", octaves, 1, 8);
        detailSize = (int)EditorGUILayout.Slider("Detail Size", detailSize, 1, 5);
        layerWeight = EditorGUILayout.Slider("Layer Weight", layerWeight, 0f, 1f);
        EditorGUILayout.Separator();
        power = EditorGUILayout.Slider("Power", power, 0f, 1f);
        brightness = EditorGUILayout.Slider("Brightness", brightness, 0f, 2f);
        contrast = EditorGUILayout.Slider("Contrast", contrast, 0f, 3f);
        EditorGUILayout.Separator();
        scratchCount = EditorGUILayout.Slider("Stroke Count", scratchCount, 1f, 6f);
        squiggles = EditorGUILayout.Slider("Squiggles", squiggles, 0f, 3f);
        sides = EditorGUILayout.Slider("Sides", sides, 0f, 1f);


        EditorGUILayout.LabelField("Octave Blend Modes", EditorStyles.centeredGreyMiniLabel);
        if (typeButton("Additive", type == 0))
        {
            type = 0;
        }
        if (typeButton("Blend", type == 1))
        {
            type = 1;
        }
        if (typeButton("Max", type == 2))
        {
            type = 2;
        }
        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class stripeSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Stripe";
    }
    public int lineCount = 10;
    public float lineAngle = 0;
    UniNoiseWindow myWindow;
    Material myMat;

    void reset()
    {
        myWindow.selectedInfo = new stripeSettings(myWindow);
    }

    public stripeSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/LinesShader"));
        myWindow = window;
        updateImage();
    }


    public override void updateImage()
    {
        
        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myWindow.blitmat = myMat;
        myMat.SetInt("_LineCount", lineCount);
        myMat.SetFloat("_LineAngle", lineAngle);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        lineCount = (int)EditorGUILayout.Slider("Line Count", lineCount, 1, 100);
        lineAngle = (int)EditorGUILayout.Slider("Line Angle", lineAngle, 0, 3);
        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class DGN : GenOrEditClass
{
    public override string getTextureName()
    {
        return "DGN";
    }

    void reset()
    {
        myWindow.selectedInfo = new DGN(myWindow);
    }

    public float seed = 0;
    public int scale = 10;
    public int octaves = 10;
    public float detail = 1;
    public float detailWeight = .95f;
    public float contrast = 1;
    UniNoiseWindow myWindow;
    Material myMat;
    public float secondaryScale = 1;
    public int type = 0;
    public float gain = 0;
    public bool turbulent = false;
    public float power = 1;

    public DGN(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/DGN"));
        myWindow = window;
        updateImage();
    }

    public override void updateImage()
    {

        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myWindow.blitmat = myMat;
        myMat.SetFloat("seed", seed);
        myMat.SetInt("tiles", scale);
        myMat.SetInt("octaves", octaves);
        myMat.SetInt("type", type);
        myMat.SetFloat("detailWeight", detailWeight);
        myMat.SetFloat("lacunarity", detail);
        myMat.SetFloat("contrast", contrast);
        myMat.SetFloat("secondaryScale", secondaryScale);
        myMat.SetFloat("gain", gain);
        myMat.SetInt("turbulent", turbulent ? 1 : 0);
        myMat.SetFloat("power", power);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        seed = EditorGUILayout.Slider("Seed", seed, 0f, 10f);
        octaves = (int)EditorGUILayout.Slider("Octaves", octaves, 1, 20);
        scale = (int)EditorGUILayout.Slider("Scale", scale, 2, 100);
        turbulent = EditorGUILayout.Toggle("Turbulent", turbulent);
        GUI.color = octaves == 1 ? Color.red : Color.white;
        detail = (int)EditorGUILayout.Slider("Detail", detail, 1f, 5f);
        detailWeight = EditorGUILayout.Slider("Detail Weight", detailWeight, 0f, 1f);
        GUI.color = Color.white;
        contrast = EditorGUILayout.Slider("Contrast", contrast, 0f, 3f);
        gain = EditorGUILayout.Slider("Gain", gain, -1f, 1f);
        if (type == 1)
        {
            GUI.color = Color.green;
            power = EditorGUILayout.Slider("Power", power, 0f, 1f);
            GUI.color = Color.white;
        }


        EditorGUILayout.BeginHorizontal();
        if (typeButton("Basic", type == 0))
        {
            type = 0;
        }
        if (typeButton("Warp", type == 1))
        {
            type = 1;
        }
        EditorGUILayout.EndHorizontal();


        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class marchCircleSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "March Circles";
    }

    void reset()
    {
        myWindow.selectedInfo = new marchCircleSettings(myWindow);
    }

    public float seed = 0;
    public int scale = 10;
    public int octaves = 5;
    public float detail = 1;
    public float detailWeight = .95f;
    public float contrast = 1;
    UniNoiseWindow myWindow;
    Material myMat;
    public float secondaryScale = 1;
    public int type = 0;
    public float percent = 1;
    public float gain = 0;

    public marchCircleSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/MarchingCircles"));
        myWindow = window;
        updateImage();
    }

    public override void updateImage()
    {

        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myWindow.blitmat = myMat;
        myMat.SetFloat("seed", seed);
        myMat.SetInt("tiles", scale);
        myMat.SetInt("octaves", octaves);
        myMat.SetInt("type", type);
        myMat.SetFloat("detailWeight", detailWeight);
        myMat.SetFloat("detail", detail);
        myMat.SetFloat("contrast", contrast);
        myMat.SetFloat("secondaryScale", secondaryScale);
        myMat.SetFloat("percent", percent);
        myMat.SetFloat("gain", gain);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        seed = EditorGUILayout.Slider("Seed", seed, 0f, 100f);
        octaves = (int)EditorGUILayout.Slider("Octaves", octaves, 1, 20);
        scale = (int)EditorGUILayout.Slider("Scale", scale, 2, 100);
        percent = EditorGUILayout.Slider("Percent", percent, .5f, 2f);
        detail = (int)EditorGUILayout.Slider("Detail", detail, 1f, 5f);
        detailWeight = EditorGUILayout.Slider("Detail Weight", detailWeight, 0f, 1f);
        contrast = EditorGUILayout.Slider("Contrast", contrast, 0f, 4f);
        gain = EditorGUILayout.Slider("Gain", gain, -1f, 1f);
        if (type == 1)
        {
            secondaryScale = (int)EditorGUILayout.Slider("Lines Scale", secondaryScale, 2, 100);
        }
        EditorGUILayout.BeginHorizontal();
        if (typeButton("March", type == 0))
        {
            type = 0;
        }
        if (typeButton("Lines", type == 1))
        {
            type = 1;
        }
        EditorGUILayout.EndHorizontal();


        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class marchSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "March Noise";
    }

    void reset()
    {
        myWindow.selectedInfo = new marchSettings(myWindow);
    }

    public float seed = 0;
    public int scale = 10;
    public int octaves = 5;
    public float detail = 1;
    public float detailWeight = .95f;
    public float contrast = 1;
    UniNoiseWindow myWindow;
    Material myMat;
    public float secondaryScale = 1;
    public int type = 0;
    public float percent = 1;
    public float gain = 0;

    public marchSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/MarchSquareShader"));
        myWindow = window;
        updateImage();
    }

    public override void updateImage()
    {

        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myWindow.blitmat = myMat;
        myMat.SetFloat("seed", seed);
        myMat.SetInt("tiles", scale);
        myMat.SetInt("octaves", octaves);
        myMat.SetInt("type", type);
        myMat.SetFloat("detailWeight", detailWeight);
        myMat.SetFloat("detail", detail);
        myMat.SetFloat("contrast", contrast);
        myMat.SetFloat("secondaryScale", secondaryScale);
        myMat.SetFloat("percent", percent);
        myMat.SetFloat("gain", gain);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        seed = EditorGUILayout.Slider("Seed", seed, 0f, 100f);
        octaves = (int)EditorGUILayout.Slider("Octaves", octaves, 1, 20);
        scale = (int)EditorGUILayout.Slider("Scale", scale, 2, 100);
        percent = EditorGUILayout.Slider("Percent", percent, .5f, 1.5f);
        detail = (int)EditorGUILayout.Slider("Detail", detail, 1f, 5f);
        detailWeight = EditorGUILayout.Slider("Detail Weight", detailWeight, 0f, 1f);
        contrast = EditorGUILayout.Slider("Contrast", contrast, 0f, 2f);
        gain = EditorGUILayout.Slider("Gain", gain, -1f, 1f);
        if (type == 1)
        {
            secondaryScale = (int)EditorGUILayout.Slider("Lines Scale", secondaryScale, 2, 100);
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        if (typeButton("March", type == 0))
        {
            type = 0;
        }
        if (typeButton("Lines", type == 1))
        {
            type = 1;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical();

        if (typeButton("Smooth", type == 3))
        {
            type = 3;
        }
        if (typeButton("Smooth Lines", type == 4))
        {
            type = 4;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();


        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class newTruchet : GenOrEditClass
{
    public float scale = 5;
    public float randomization = 10;
    public float octaves = 1;
    public bool points = false;
    public int overrideTile = -1;

    public Vector4 testPoint;

    public override string getTextureName()
    {
        return "Truchet";
    }

    void reset()
    {
        myWindow.selectedInfo = new newTruchet(myWindow);
    }

    public float seed = 0;
    public int type = 0;
    UniNoiseWindow myWindow;
    Material myMat;

    public newTruchet(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/Truchet"));
        myWindow = window;
        updateImage();
    }

    public override void updateImage()
    {

        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myMat.SetFloat("tiles", scale);
        myMat.SetFloat("randomization", randomization);
        myMat.SetFloat("points", points ? 2 : 1);
        myMat.SetFloat("octaves", octaves);
        myMat.SetInt("type", type);
        myMat.SetInt("overrideTile", overrideTile);
        myMat.SetVector("testPoint", testPoint);
        myWindow.blitmat = myMat;
        //myMat.SetFloat("seed", seed);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        
        scale = (int)EditorGUILayout.Slider("Scale", scale, 1f, 20f);
        if (type == 0)
        {
            octaves = (int)EditorGUILayout.Slider("Octaves", octaves, 1, 12);
            points = EditorGUILayout.Toggle("Points", points);
        }
        randomization = EditorGUILayout.Slider("Seed", randomization, 0f, 1000f);

        if (type == 3)
        {
            overrideTile = (int)EditorGUILayout.Slider("Override Tile", overrideTile, -1, 10);
        }
        //testPoint = EditorGUILayout.Vector4Field("testPoint", testPoint);
        //test = EditorGUILayout.Toggle("Test", test);

        if (typeButton("Basic", type == 0))
        {
            type = 0;
        }
        if (typeButton("Triangles", type == 1))
        {
            type = 1;
        }
        if (typeButton("Segments", type == 2))
        {
            type = 2;
        }
        if (typeButton("Multi Tile", type == 3))
        {
            type = 3;
        }

        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class plaidSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Plaid";
    }

    void reset()
    {
        myWindow.selectedInfo = new plaidSettings(myWindow);
    }

    public float scale = 2;
    public int type = 0;
    public int plaidCount = 4;
    public float plainMinWidth = .05f;
    public float plaidMaxWidth = .1f;
    UniNoiseWindow myWindow;
    Material myMat;
    List<Vector4> plaids;

    public plaidSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoisePlaid"));
        myWindow = window;
        getPlaids();
        updateImage();
    }

    void getPlaids()
    {
        plaids = new List<Vector4>();
        for(int i = 0; i< 20; i++)
        {
            plaids.Add (new Vector4(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(plainMinWidth, plaidMaxWidth)));
        }
    }

    public override void updateImage()
    {

        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myWindow.blitmat = myMat;
        myMat.SetFloat("scale", scale*2);
        myMat.SetInt("type", type);
        myMat.SetVectorArray("plaids", plaids.ToArray());
        myMat.SetInt("plaidCount", plaidCount);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        scale = (int)EditorGUILayout.Slider("Scale", scale, 2f, 20f);

        EditorGUI.BeginChangeCheck();
        if (type == 2)
        {
            plaidCount = (int)EditorGUILayout.Slider("Plaid Count", plaidCount, 1f, 20f);
            EditorGUILayout.MinMaxSlider("Plaid Size", ref plainMinWidth, ref plaidMaxWidth, 0f, .5f);
            if (EditorGUI.EndChangeCheck())
            {
                getPlaids();
            }
        }

        if (typeButton("Squares", type == 0))
        {
            type = 0;
        }
        if (typeButton("2-Tone", type == 1))
        {
            type = 1;
        }
        if (typeButton("Boxes", type == 2))
        {
            getPlaids();
            type = 2;
        }

        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class houndstoothSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Houndstooth";
    }

    void reset()
    {
        myWindow.selectedInfo = new houndstoothSettings(myWindow);
    }

    public float scale = 2;
    public int type = 0;
    UniNoiseWindow myWindow;
    Material myMat;

    public houndstoothSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseHoundstooth"));
        myWindow = window;
        updateImage();
    }

    public override void updateImage()
    {

        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myWindow.blitmat = myMat;
        myMat.SetFloat("scale", type == 1 ? scale * 4 : scale*2);
        myMat.SetInt("type", type);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        scale = (int)EditorGUILayout.Slider("Scale", scale, 1f, 10f);

        if (typeButton("2-Tone", type == 0))
        {
            type = 0;
        }
        if (typeButton("Smooth1", type == 1))
        {
            type = 1;
        }
        if (typeButton("Smooth2", type == 2))
        {
            type = 2;
        }
        if (typeButton("Tile Distance", type == 3))
        {
            type = 3;
        }

        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class dotsSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Dots";
    }

    void reset()
    {
        myWindow.selectedInfo = new dotsSettings(myWindow);
    }

    public int scale = 1;
    public int type = 0;
    UniNoiseWindow myWindow;
    Material myMat;
    public float scaleMulti = 2;

    public dotsSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseDots"));
        myWindow = window;
        updateImage();
    }

    public override void updateImage()
    {

        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myWindow.blitmat = myMat;
        myMat.SetFloat("scale", scale);
        myMat.SetInt("type", type);
        myMat.SetFloat("scaleMulti", scaleMulti);
        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        scale = (int)EditorGUILayout.Slider("Scale", scale, 2f, 20f);
        scaleMulti = EditorGUILayout.Slider("Dot Size", scaleMulti, 2f, 15f);

        if (typeButton("Dots", type == 0))
        {
            type = 0;
        }
        if (typeButton("Offset", type == 1))
        {
            type = 1;
        }
        if (typeButton("Offset 2 Tone", type == 2))
        {
            type = 2;
        }
        if (typeButton("Offset 3 Tone", type == 3))
        {
            type = 3;
        }
        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class scalesSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Scales";
    }

    void reset()
    {
        myWindow.selectedInfo = new scalesSettings(myWindow);
    }

    public float xScale = 2;
    public float yScale = 2;
    public float power = 1;
    public int type = 0;
    UniNoiseWindow myWindow;
    Material myMat;

    public scalesSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseScales"));
        myWindow = window;
        updateImage();
    }

    public override void updateImage()
    {

        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myWindow.blitmat = myMat;
        myMat.SetFloat("yScale", yScale);
        myMat.SetFloat("xScale", xScale);
        myMat.SetFloat("power", power);
        myMat.SetInt("type", type);
        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        xScale = (int)EditorGUILayout.Slider("X Scale", xScale, 2f, 20f);
        yScale = (int)EditorGUILayout.Slider("Y Scale", yScale, 2f, 20f);
        if (type > 1)
        {
            power = EditorGUILayout.Slider("Power", power, 1f, 20f);
        }

        if (typeButton("Flat", type == 0))
        {
            type = 0;
        }
        if (typeButton("Smooth", type == 1))
        {
            type = 1;
        }
        if (typeButton("Sined", type == 2))
        {
            type = 2;
        }
        if (typeButton("Sined Flipped", type == 3))
        {
            type = 3;
        }
        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class noiseSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Basic Noise";
    }

    void reset()
    {
        myWindow.selectedInfo = new noiseSettings(myWindow);
    }

    public float seed = 0;
    UniNoiseWindow myWindow;
    Material myMat;

    public noiseSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/UniBasicNoise"));
        myWindow = window;
        updateImage();
    }

    public override void updateImage()
    {

        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myWindow.blitmat = myMat;
        myMat.SetFloat("seed", seed);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        seed = EditorGUILayout.Slider("Seed", seed, 0f, 100f);
        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class checkerSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Checkers";
    }

    void reset()
    {
        myWindow.selectedInfo = new checkerSettings(myWindow);
    }

    public int octaves = 10;

    public float wobbleX = 0;
    public float wobbleY = 0;
    public int wobbleSize = 1;
    public float tear = 0;
    public float sphere = 0;
    UniNoiseWindow myWindow;
    Material myMat;
    public bool useGradient = false;

    public checkerSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseCheckers"));
        myWindow = window;
        updateImage();
    }

    public override void updateImage()
    {
        
        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myWindow.blitmat = myMat;
        myMat.SetInt("_Octaves", octaves);
        myMat.SetFloat("_Wobble", wobbleX);
        myMat.SetFloat("_Wobble2", wobbleY);
        myMat.SetFloat("_WobbleSize", wobbleSize);
        myMat.SetFloat("_Teardrop", tear);
        myMat.SetFloat("_Sphere", sphere);
        myMat.SetInt("_Type", useGradient == false ? 0 : 1);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        useGradient = EditorGUILayout.Toggle("Use Gradient",useGradient);
        octaves = (int)EditorGUILayout.Slider("Octaves", octaves, 1, 20);
        wobbleX = EditorGUILayout.Slider("Wobble X", wobbleX, 0f, .5f);
        wobbleY = EditorGUILayout.Slider("Wobble Y", wobbleY, 0f, .5f);
        wobbleSize = (int)EditorGUILayout.Slider("Wobble Size", wobbleSize, 1, 4);
        tear = EditorGUILayout.Slider("Teardrop", tear, 0f, .5f);
        sphere = EditorGUILayout.Slider("Sphere", sphere, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class gridSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Grid";
    }

    void reset()
    {
        myWindow.selectedInfo = new gridSettings(myWindow);
    }

    public int octaves = 10;

    UniNoiseWindow myWindow;
    Material myMat;
    public int _Type = 0;
    public float _Width = .1f;

    public gridSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseGrid"));
        myWindow = window;
        updateImage();
    }

    public override void updateImage()
    {
        
        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myWindow.blitmat = myMat;
        myMat.SetInt("_Octaves", octaves);
        myMat.SetInt("_Type", _Type);
        myMat.SetFloat("_Width", _Width);
        
        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());

        base.applyGeneration(myWindow,myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        _Type = (int)EditorGUILayout.Slider("Type", _Type, 0, 4);
        octaves = (int)EditorGUILayout.Slider("Octaves", octaves, 1, 20);
        _Width=EditorGUILayout.Slider("Width", _Width, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class BoxNoiseSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Box Noise";
    }

    void reset()
    {
        myWindow.selectedInfo = new BoxNoiseSettings(myWindow);
    }

    public int scale = 5;
    UniNoiseWindow myWindow;
    Material myMat;

    public BoxNoiseSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseNormalBoxes"));
        myWindow = window;
        scale = 10;
        updateImage();
    }

    Vector2[,] pPoints;
    Vector4[] flatPoints;
    int lastScale = 0;
    Vector2[,] getPoints()
    {
        if (myMat != null)
        {
            myWindow.destroyObj(myMat);
        }
        myMat = new Material(Shader.Find("Hidden/UniNoiseNormalBoxes"));
        int tileCount = Mathf.CeilToInt(scale) + 1;

        Vector2[,] pPoints = new Vector2[tileCount, tileCount];
        flatPoints = new Vector4[tileCount * tileCount];
        for (int x = 0; x < pPoints.GetLength(0); x++)
        {
            for (int y = 0; y < pPoints.GetLength(1); y++)
            {
                pPoints[x, y] = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));
                flatPoints[(x * tileCount) + y] = pPoints[x, y];
            }
        }

        

        return pPoints;
    }

    public override void updateImage()
    {
        

        Texture2D newTex = myWindow.blankTex();

        if (pPoints == null || scale != lastScale)
        {
            pPoints = getPoints();
            lastScale = scale;
        }

        myMat.SetInt("_Octaves", scale);
        myMat.SetInt("_TextureSize", myWindow.textureSize);
        myMat.SetVectorArray("_points", flatPoints);
        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        scale = (int)EditorGUILayout.Slider("Points", scale, 1, 30);
        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class spacialNoiseSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Spacial";
    }

    void reset()
    {
        myWindow.selectedInfo = new spacialNoiseSettings(myWindow);
    }

    public int pointCount = 5;
    public float scale = 5f;
    UniNoiseWindow myWindow;
    Material myMat;
    float[] currentPoints;
    public bool saw = true;

    public float power = 1;
    public spacialNoiseSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/SpacialShader"));
        myWindow = window;
        pointCount = 5;
        scale = 5f;
        updateImage();
    }

    public float[] pointPositions(int size)
    {
        if (currentPoints != null && currentPoints.Length == pointCount*3) { return currentPoints; }
        if (myMat)
        {
            myWindow.destroyObj(myMat);
        }
        myMat = new Material(Shader.Find("Hidden/SpacialShader"));


        List<float> points = new List<float>();
        for (int i = 0; i < size; i++)
        {
            points.Add(Random.Range(0f, 1f));
            points.Add(Random.Range(0f, 1f));
            points.Add(Random.Range(0f, 1f));
        }
        currentPoints = points.ToArray();
        return points.ToArray();
    }

    public override void updateImage()
    {
        currentPoints = pointPositions(pointCount);
        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myWindow.blitmat = myMat;
        myMat.SetInt("_VectorCount", pointCount);
        myMat.SetFloat("_Scale", scale);
        myMat.SetFloatArray("_points", currentPoints);
        myMat.SetInt("_Type", saw ? 0 : 1);
        myMat.SetFloat("power", power);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        saw = EditorGUILayout.Toggle("Saw", saw);
        pointCount = (int)EditorGUILayout.Slider("Points", pointCount, 1, 20);
        scale = EditorGUILayout.Slider("Scale", scale, .5f, 100f);
        power = EditorGUILayout.Slider("Power", power, 0f, 1f);

        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class ParticleizeSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Particleized";
    }

    void reset()
    {
        myWindow.selectedEdit = new ParticleizeSettings(myWindow, inputTex);
    }

    public float power = 1f;
    public float threshold = 1;
    public float size = 10;
    public bool preserveColor = true;
    public bool hardCut = false;
    public bool square = false;
    public float contrast = 1;
    public bool singleColor;
    public Color singleColorColor;
    Color backgroundColor;
    UniNoiseWindow myWindow;
    Material myMat;
    Texture2D inputTex;
    public ParticleizeSettings(UniNoiseWindow window, Texture2D inputTexture)
    {
        base.hasPreviewImageUpdate = true;
        myMat = new Material(Shader.Find("Hidden/UniNoiseParticleize"));
        inputTex = inputTexture;
        myWindow = window;
        backgroundColor = Color.grey;
        updateImage();
    }

    public override void setInputTex(Texture2D tex)
    {
        inputTex = tex;
    }

    public override void updateImage()
    {

        myWindow.blitmat = myMat;
        myMat.SetFloat("power", power);
        myMat.SetFloat("_TextureSize", myWindow.textureSize);
        myMat.SetTexture("_InputTex", inputTex);
        myMat.SetFloat("threshold", threshold);
        myMat.SetInt("preserveColor", preserveColor ? 1 : 0);
        myMat.SetInt("hardCut", hardCut ? 1 : 0);
        myMat.SetInt("square", square ? 1 : 0);
        myMat.SetFloat("size", size);
        myMat.SetInt("doCheckers", 1); // checkers for preview
        myMat.SetFloat("contrast", contrast);
        myMat.SetColor("backgroundColor", backgroundColor);
        myMat.SetInt("singleColor", singleColor ? 1 : 0);
        myMat.SetVector("singleColorColor", singleColorColor);


        base.applyGeneration(myWindow, myMat);
    }

    public override void updateImageFinal()
    {
        myWindow.blitmat = myMat;
        myMat.SetFloat("power", power);
        myMat.SetFloat("_TextureSize", myWindow.textureSize);
        myMat.SetTexture("_InputTex", inputTex);
        myMat.SetFloat("threshold", threshold);
        myMat.SetInt("preserveColor", preserveColor ? 1 : 0);
        myMat.SetInt("hardCut", hardCut ? 1 : 0);
        myMat.SetInt("square", square ? 1 : 0);
        myMat.SetFloat("size", size);
        myMat.SetInt("doCheckers", 0); // no checkers for final image
        myMat.SetFloat("contrast", contrast);
        myMat.SetInt("singleColor", singleColor ? 1 : 0);
        myMat.SetVector("singleColorColor", singleColorColor);


        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        power = EditorGUILayout.Slider("Power", power, .2f, 10f);
        size = EditorGUILayout.Slider("Size", size, 1f, 10f);
        threshold = EditorGUILayout.Slider("Threshold", threshold, 0f, 2f);
        contrast = EditorGUILayout.Slider("Contrast", contrast, 0f, 5f);
        preserveColor = EditorGUILayout.Toggle("Preserve Color", preserveColor);
        hardCut = EditorGUILayout.Toggle("Hard Cut", hardCut);
        square = EditorGUILayout.Toggle("Square", square);
        backgroundColor = EditorGUILayout.ColorField("Transparency Preview Color",backgroundColor);

        singleColor = EditorGUILayout.Toggle("Single Color", singleColor);

        if (singleColor)
        {
            singleColorColor = EditorGUILayout.ColorField("Single Color", singleColorColor);
        }



        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class BlurSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Blurred";
    }

    void reset()
    {
        myWindow.selectedEdit = new BlurSettings(myWindow, inputTex);
    }

    public float blur = 3f;
    public float blurSpread = 1f;
    public float threshold = 1;
    UniNoiseWindow myWindow;
    Material myMat;
    Texture2D inputTex;
    public int Quality = 4;
    public BlurSettings(UniNoiseWindow window, Texture2D inputTexture)
    {
        myMat = new Material(Shader.Find("Hidden/TexBlurShader"));
        inputTex = inputTexture;
        myWindow = window;
        updateImage();
    }

    public override void setInputTex(Texture2D tex)
    {
        inputTex = tex;
    }

    public override void updateImage()
    {
        
        myWindow.blitmat = myMat;
        myMat.SetFloat("_Blur", blur);
        myMat.SetFloat("_BlurSpread", blurSpread);
        myMat.SetFloat("_TextureSize", myWindow.textureSize);
        myMat.SetTexture("_InputTex", inputTex);
        myMat.SetFloat("_Threshold", threshold);
        myMat.SetInt("_Quality", Quality);


        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        blur = EditorGUILayout.Slider("Blur", blur, 0f, 1f);
        blurSpread = EditorGUILayout.Slider("Spread", blurSpread, 1, 10);
        threshold = EditorGUILayout.Slider("Threshold", threshold, 0f, 1f);
        Quality = (int)EditorGUILayout.Slider("Quality", Quality, 1, 20);


        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class warpSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Warped";
    }

    void reset()
    {
        myWindow.selectedEdit = new warpSettings(myWindow, inputTex);
    }


    UniNoiseWindow myWindow;
    Material myMat;
    Texture2D inputTex;
    Texture2D warpTexX;
    Texture2D warpTexY;

    public float powerX = 0;
    public float powerY = 0;
    public float overallMultiplier = 1;
    public int wobblSizeX = 1;
    public int wobblSizeY = 1;
    public float wobbleX = 0;
    public float wobbleY = 0;

    public warpSettings(UniNoiseWindow window, Texture2D inputTexture)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseWarp"));
        inputTex = inputTexture;
        myWindow = window;
        updateImage();
    }

    public override void setInputTex(Texture2D tex)
    {
        inputTex = tex;
    }

    public override void updateImage()
    {

        myWindow.blitmat = myMat;
        myMat.SetTexture("_InputTex", inputTex);
        myMat.SetTexture("warpX", warpTexX);
        myMat.SetTexture("warpY", warpTexY);
        myMat.SetFloat("powerX", powerX);
        myMat.SetFloat("powerY", powerY);
        myMat.SetFloat("wobblex", wobbleX);
        myMat.SetFloat("wobbley", wobbleY);
        myMat.SetInt("wobbleSizeX", wobblSizeX);
        myMat.SetInt("wobbleSizeY", wobblSizeY);
        myMat.SetFloat("overallMultiplier", overallMultiplier);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Wobble Size", GUILayout.Width(95));
        wobblSizeX = (int)EditorGUILayout.Slider(wobblSizeX, 1f, 10f);
        wobblSizeY = (int)EditorGUILayout.Slider(wobblSizeY, 1f, 10f);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Wobble Amount", GUILayout.Width(95));
        wobbleX = EditorGUILayout.Slider(wobbleX, 0f, 1f);
        wobbleY = EditorGUILayout.Slider(wobbleY, 0f, 1f);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("-Warp With Images-", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.BeginHorizontal();

        warpTexX = EditorGUILayout.ObjectField("Warp X",warpTexX, typeof(Texture2D), false) as Texture2D;
        warpTexY = EditorGUILayout.ObjectField("Warp Y", warpTexY, typeof(Texture2D), false) as Texture2D;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        powerX = EditorGUILayout.Slider(powerX, - 1f, 1f);
        powerY = EditorGUILayout.Slider(powerY, -1f, 1f);

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(25);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);


        overallMultiplier = EditorGUILayout.Slider("Total Warp",overallMultiplier, 0f, 2f);

        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class ColorizeSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Colorized";
    }

    void reset()
    {
        myWindow.selectedEdit = new ColorizeSettings(myWindow, inputTex);
    }

    UniNoiseWindow myWindow;
    Material myMat;
    Texture2D inputTex;
    Color mulCol;
    float _Mul = 1;
    public ColorizeSettings(UniNoiseWindow window, Texture2D inputTexture)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseColorize"));
        inputTex = inputTexture;
        myWindow = window;
        updateImage();
    }

    public override void setInputTex(Texture2D tex)
    {
        inputTex = tex;
    }

    public override void updateImage()
    {

        myWindow.blitmat = myMat;
        myMat.SetTexture("_InputTex", inputTex);
        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myMat.SetColor("_Color", mulCol);
        myMat.SetFloat("_Mul", _Mul);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();

        //myWindow.mainGradient = EditorGUILayout.GradientField(myWindow.mainGradient);
        mulCol = EditorGUILayout.ColorField(mulCol);
        _Mul = EditorGUILayout.FloatField("Multiplier", _Mul);

        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class ColorRampSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Color Ramped";
    }

    void reset()
    {
        myWindow.selectedEdit = new ColorRampSettings(myWindow, inputTex);
    }

    UniNoiseWindow myWindow;
    Material myMat;
    Texture2D inputTex;
    public ColorRampSettings(UniNoiseWindow window, Texture2D inputTexture)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseColorRamp"));
        inputTex = inputTexture;
        myWindow = window;
        updateImage();
    }

    public override void setInputTex(Texture2D tex)
    {
        inputTex = tex;
    }

    public override void updateImage()
    {
        
        myWindow.blitmat = myMat;
        myMat.SetTexture("_InputTex", inputTex);
        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());


        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();

        myWindow.mainGradient = EditorGUILayout.GradientField(myWindow.mainGradient);

        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class OutlineSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Outlined";
    }

    void reset()
    {
        myWindow.selectedEdit = new OutlineSettings(myWindow, inputTex);
    }

    public float distance = 1f;
    public float power = .331f;
    UniNoiseWindow myWindow;
    Material myMat;
    Texture2D inputTex;
    public float blend = 1f;
    public float threshold = .9f;

    public OutlineSettings(UniNoiseWindow window, Texture2D inputTexture)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseOutlineShader"));
        inputTex = inputTexture;
        myWindow = window;
        updateImage();
    }

    public override void setInputTex(Texture2D tex)
    {
        inputTex = tex;
    }

    public override void updateImage()
    {

        myWindow.blitmat = myMat;
        myMat.SetFloat("dist", distance);
        myMat.SetFloat("power", power);
        myMat.SetFloat("textureSize", myWindow.textureSize);
        myMat.SetTexture("_InputTex", inputTex);
        myMat.SetFloat("blend", blend);
        myMat.SetFloat("threshold", threshold);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        threshold = EditorGUILayout.Slider("Threshold", threshold, 0f, 1f);
        power = EditorGUILayout.Slider("Power", power, 0f, 2f);
        distance = EditorGUILayout.Slider("Distance", distance, .05f, 10f);
        blend = EditorGUILayout.Slider("Blend", blend, 0f, 1f);


        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class NormalSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Normalified";
    }

    void reset()
    {
        myWindow.selectedEdit = new NormalSettings(myWindow, inputTex);
    }

    public float distance = 1f;
    public float power = .331f;
    UniNoiseWindow myWindow;
    Material myMat;
    Texture2D inputTex;
    public float quality = 4;

    public NormalSettings(UniNoiseWindow window, Texture2D inputTexture)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseNormalShader"));
        inputTex = inputTexture;
        myWindow = window;
        updateImage();
    }

    public override void setInputTex(Texture2D tex)
    {
        inputTex = tex;
    }

    public override void updateImage()
    {
        
        myWindow.blitmat = myMat;
        myMat.SetFloat("_Distance", distance);
        myMat.SetFloat("_Power", power);
        myMat.SetFloat("_TextureSize", myWindow.textureSize);
        myMat.SetTexture("_InputTex", inputTex);
        myMat.SetFloat("quality", quality);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        power = EditorGUILayout.Slider("Power", power, 0f, 2f);
        distance = EditorGUILayout.Slider("Distance", distance, .1f, 10f);
        quality = EditorGUILayout.Slider("Quality", quality, 1, 15);


        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class BlendSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Normalified";
    }

    void reset()
    {
        myWindow.selectedEdit = new BlendSettings(myWindow, inputTex);
    }

    UniNoiseWindow myWindow;
    Material myMat;
    Texture2D inputTex;
    Texture2D blendTex;
    Texture2D blendTex2;

    public float blend = .5f;
    public float threshold = 1;
    public int type = 0;

    public BlendSettings(UniNoiseWindow window, Texture2D inputTexture)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseBlendTextures"));
        inputTex = inputTexture;
        myWindow = window;
        updateImage();
    }

    public override void setInputTex(Texture2D tex)
    {
        inputTex = tex;
    }

    public override void updateImage()
    {

        myWindow.blitmat = myMat;
        myMat.SetFloat("blend", blend);
        myMat.SetFloat("threshold", threshold);
        myMat.SetFloat("_TextureSize", myWindow.textureSize);
        myMat.SetTexture("_InputTex", inputTex);
        myMat.SetTexture("blendTex", blendTex);
        myMat.SetTexture("blendTex2", blendTex2);
        myMat.SetInt("type", type);

        base.applyGeneration(myWindow, myMat);
    }

    

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        if (blendTex != null)
        {
            if (GUILayout.Button("Swap Input and blend"))
            {
                Texture2D hold = inputTex;
                inputTex = blendTex;
                blendTex = hold;
                myWindow.texToEdit = inputTex;
                updateImage();
            }
        }
        EditorGUI.BeginChangeCheck();
        blend = EditorGUILayout.Slider(type == 0 ? "Blend" : "Offset", blend, 0f, 1f);
        threshold = EditorGUILayout.Slider(type == 0 ? "Threshold" : "Contrast", threshold, 0f, 1f);
        blendTex = EditorGUILayout.ObjectField("Blend Texture", blendTex, typeof(Texture2D), false) as Texture2D;
        if (type == 1)
        {
            blendTex2 = EditorGUILayout.ObjectField("Blend Texture2", blendTex2, typeof(Texture2D), false) as Texture2D;
        }

        EditorGUILayout.BeginHorizontal();
        if (base.typeButton("Mix", type == 0)){
            type = 0;
        }
        if (base.typeButton("Blend 2 by input", type == 1))
        {
            type = 1;
        }
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class TriGridSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Triangles";
    }

    void reset()
    {
        myWindow.selectedInfo = new TriGridSettings(myWindow);
    }

    public float scaleX = 3;
    public float scaleY = 3;
    public float wobblex = 0;
    public float wobbley = 0;
    public int wobbleSize = 1;
    public bool flipHalfY;
    public int type = 0;
    UniNoiseWindow myWindow;
    Material myMat;
    public TriGridSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseTriGrid"));
        myWindow = window;
        updateImage();
    }

    public override void updateImage()
    {

        myWindow.blitmat = myMat;
        myMat.SetFloat("scaleX", scaleX);
        myMat.SetFloat("scaleY", scaleY);
        myMat.SetFloat("wobble", wobblex);
        myMat.SetFloat("wobble2", wobbley);
        myMat.SetInt("wobbleSize", wobbleSize);
        myMat.SetInt("type", type);
        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myMat.SetInt("flipHalfY", flipHalfY ? 1 : 0);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        scaleX = EditorGUILayout.Slider("Scale X", scaleX, 1f, 10f);
        scaleY = EditorGUILayout.Slider("Scale Y", scaleY, 1f, 10f);
        wobblex = EditorGUILayout.Slider("Wobble X", wobblex, 0f, 1f);
        wobbley = EditorGUILayout.Slider("Wobble Y", wobbley, 0f, 1f);
        wobbleSize = (int)EditorGUILayout.Slider("Wobble Size", wobbleSize, 1, 5);
        flipHalfY = EditorGUILayout.Toggle("Flip Half Y", flipHalfY);

        EditorGUILayout.BeginHorizontal();
        if (typeButton("Tris", type == 0))
        {
            type = 0;
        }
        if (typeButton("Tri Point", type == 1))
        {
            type = 1;
        }
        if (typeButton("Hex", type == 2))
        {
            type = 2;
        }
        if (typeButton("Tri Point 2", type == 3))
        {
            type = 3;
        }
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class CubeSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Cubes";
    }

    void reset()
    {
        myWindow.selectedInfo = new CubeSettings(myWindow);
    }

    public int scale = 3;
    public int type = 0;
    public float topValue = 0;
    public float leftValue = .5f;
    public float rightValue = 1;
    public float smoothPower = 1;
    UniNoiseWindow myWindow;
    Material myMat;
    public CubeSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseCubes"));
        myWindow = window;
        updateImage();
    }

    public override void updateImage()
    {

        myWindow.blitmat = myMat;
        myMat.SetInt("scale", scale);
        //myMat.SetFloat("_TextureSize", myWindow.textureSize);
        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myMat.SetInt("type", type);
        myMat.SetFloat("rightValue", rightValue);
        myMat.SetFloat("topValue", topValue);
        myMat.SetFloat("leftValue", leftValue);
        myMat.SetFloat("smoothPower", smoothPower);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        scale = (int)EditorGUILayout.Slider("Scale", scale, 1f, 20f);
        topValue = EditorGUILayout.Slider("Top Position", topValue, 0f, 1f);
        leftValue = EditorGUILayout.Slider("Left Position", leftValue, 0f, 1f);
        rightValue = EditorGUILayout.Slider("Right Position", rightValue, 0f, 1f);
        smoothPower = EditorGUILayout.Slider("Smooth Gradient Multiplier", smoothPower, 0f, 10f);

        if (typeButton("Basic", type == 0))
        {
            type = 0;
        }
        if (typeButton("Smooth", type == 1))
        {
            type = 1;
        }
        if (typeButton("Smooth Basic Top", type == 2))
        {
            type = 2;
        }
        if (typeButton("Hatched", type == 3))
        {
            type = 3;
        }

        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class WaveSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Waves";
    }

    void reset()
    {
        myWindow.selectedInfo = new WaveSettings(myWindow);
    }

    public int waves = 3;
    public int waves2 = 3;
    public float power = .15f;
    public int type = 0;
    UniNoiseWindow myWindow;
    Material myMat;
    public WaveSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseWaves"));
        myWindow = window;
        updateImage();
    }

    public override void updateImage()
    {
        
        myWindow.blitmat = myMat;
        myMat.SetInt("_Waves", waves);
        myMat.SetInt("_Waves2", waves2);
        myMat.SetFloat("_Power", power);
        myMat.SetFloat("_TextureSize", myWindow.textureSize);
        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());
        myMat.SetInt("type", type);

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        power = EditorGUILayout.Slider("Power", power, 0f, 1f);
        waves2 = (int)EditorGUILayout.Slider("Waves X", waves2, 1f, 20f);
        waves = (int)EditorGUILayout.Slider("Waves Y", waves, 1f, 20f);

        if (typeButton("Waves", type == 0))
        {
            type = 0;
        }
        if (typeButton("Pointy", type == 1))
        {
            type = 1;
        }
        if (typeButton("Clamped", type == 2))
        {
            type = 2;
        }
        if (typeButton("Sharp", type == 3))
        {
            type = 3;
        }

        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}

public class gradientSettings : GenOrEditClass
{
    public override string getTextureName()
    {
        return "Gradient";
    }

    void reset()
    {
        myWindow.selectedInfo = new gradientSettings(myWindow);
    }

    public float rotation = 0;
    public bool flip = false;
    public bool snapAngle = false;

    UniNoiseWindow myWindow;
    Material myMat;
    public gradientSettings(UniNoiseWindow window)
    {
        myMat = new Material(Shader.Find("Hidden/UniNoiseGradientShader"));
        myWindow = window;
        updateImage();
    }

    public override void updateImage()
    {

        myWindow.blitmat = myMat;
        myMat.SetInt("flip", flip ? 1 : 0);
        myMat.SetInt("snapAngle", snapAngle ? 1 : 0);
        myMat.SetFloat("rotation", rotation);
        myMat.SetFloat("_TextureSize", myWindow.textureSize);
        myMat.SetTexture("_GradientTex", myWindow.gradientTexture());

        base.applyGeneration(myWindow, myMat);
    }

    public override void doGUI()
    {
        if (base.genericMenuReturnsReset())
        {
            reset();
        }
        EditorGUI.BeginChangeCheck();
        rotation = EditorGUILayout.Slider("Rotation", rotation, 0f, 1f);
        flip = EditorGUILayout.Toggle("Flip", flip);
        snapAngle = EditorGUILayout.Toggle("SnapAngle", snapAngle);

        if (EditorGUI.EndChangeCheck())
        {
            updateImage();
        }
    }
}
