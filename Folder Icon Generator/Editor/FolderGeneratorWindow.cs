using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System;

public class FolderGeneratorWindow : EditorWindow
{
    public static FolderGeneratorWindow Window;


    private static Color FolderColor = new Color(0.76078431f, 0.76078431f, 0.76078431f);
    private static bool FlatTexture = false;
    private static Color IconColor = new Color(1f, 1f, 1f);
    private static Texture2D DefFolderTexture;
    private static Texture2D FolderTexture;
    private static Texture2D IconTexture;
    private static string IconName = "New Icon";
    private static Vector2 IconScale = new Vector2(1, 1);
    private static Vector2 IconOffset = new Vector2(75, 75);

    [MenuItem(itemName: "Tools/Folder Icon Generator")]
    public static void OpenWindow()
    {
        DefFolderTexture = DefaultFolderTexture();

        Window = GetWindow<FolderGeneratorWindow>();
        Window.titleContent = new GUIContent("Folder Icon Generator");
        Window.minSize = new Vector2(350, 500);
        Window.Reset();
        Window.Show();

        PreviewWindow.StartWindow();
    }

    private void OnDisable()
    {
        if (PreviewWindow.Window != null)
            PreviewWindow.Window.Close();
    }

    private void Reset()
    {
        DefFolderTexture = null;
        IconTexture = null;

        FolderColor = new Color(0.76078431f, 0.76078431f, 0.76078431f);
        FlatTexture = false;
        IconColor = new Color(1f, 1f, 1f);

        IconName = "New Icon";
        IconScale = new Vector2(1, 1);
        IconOffset = new Vector2(75, 75);
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset", GUILayout.Width(100)))
        {
            Reset();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(25);


        DrawInputCustomisations();

        if (PreviewWindow.Window != null)
            PreviewWindow.Window.Repaint();


        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Preview", GUILayout.Height(50)))
            PreviewWindow.StartWindow();
        if (GUILayout.Button("Save PNG", GUILayout.Height(75)))
        {
            Type projectWindowUtilType = typeof(ProjectWindowUtil);
            MethodInfo getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            object obj = getActiveFolderPath.Invoke(null, new object[0]);
            string pathToCurrentFolder = obj.ToString();
            string path = EditorUtility.OpenFolderPanel("Path", pathToCurrentFolder, "");

            if (string.IsNullOrEmpty(path))
                return;

            SaveIconAsPNG(path + "/" + IconName + ".png");
        }
    }


    private void DrawInputCustomisations()
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();
        Vector2 scaledIconInput = AdaptiveScaling(0.5f);

        if (IconTexture != null)
        {
            float scale = 100;

            float maxScaling = Mathf.Max(IconTexture.width, IconTexture.height);
            float ratio = scale / maxScaling;

            float width = IconTexture.width * ratio;
            float height = IconTexture.height * ratio;

            GUILayout.Label(IconTexture, GUILayout.Width(width), GUILayout.Height(height));
        }
        else
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            IconTexture = (Texture2D)EditorGUILayout.ObjectField(IconTexture, typeof(Texture2D), false, GUILayout.Width(75), GUILayout.Height(75));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Load Icon Texture"))
        {
            string path = EditorUtility.OpenFilePanel("Path", "Assets", "png, jpg");
            if (string.IsNullOrEmpty(path) == false)
            {
                byte[] bytes = File.ReadAllBytes(path);
                IconTexture = new Texture2D(0, 0);
                IconTexture.LoadImage(bytes);
            }
        }

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        float space = Mathf.Sqrt(scaledIconInput.x * scaledIconInput.y) - 250;
        GUILayout.Space(space);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        FolderColor = EditorGUILayout.ColorField("Folder Tint:", FolderColor);
        FolderTexture = ApplyColorFilter(DefFolderTexture, FolderColor);
        FlatTexture = EditorGUILayout.Toggle("Flat Icon Texture:", FlatTexture);
        IconColor = EditorGUILayout.ColorField("Icon Tint:", IconColor);
        IconName = EditorGUILayout.TextField("Icon Name:", IconName, GUILayout.Width(300));

        if (IconName == "New Icon" && IconTexture)
            IconName = IconTexture.name;

        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        if (IconTexture != null)
        {

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            IconScale = EditorGUILayout.Vector2Field("Scale:", IconScale);
            IconOffset = EditorGUILayout.Vector2Field("Offset:", IconOffset);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

        }
    }

    public void SaveIconAsPNG(string path)
    {
        int size = (int) PreviewWindow.WindowSize; // Assuming WindowSize is a float defining the desired size

        // Create a RenderTexture
        RenderTexture renderTexture = new RenderTexture(size, size, 24);
        RenderTexture.active = renderTexture;

        // Clear the render texture
        GL.Clear(true, true, Color.clear);

        // Set up the coordinate system so that (0,0) is at the bottom-left
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, size, size, 0);

        // Draw the folder texture
        Rect folderRect = new Rect(0, 0, size, size);
        Graphics.DrawTexture(folderRect, FolderTexture);

        // Calculate the icon rect
        float iconSize = size * 0.5f; // Icon is half the size of the folder icon
        Rect iconRect = new Rect(
            IconOffset.x + (size - iconSize) * 0.5f,
            IconOffset.y + (size - iconSize) * 0.5f,
            iconSize * IconScale.x,
            iconSize * IconScale.y
        );

        if (IconTexture != null)
        {
            // Draw the icon texture over the folder icon
            Texture2D readableIcon = LayerTexture(IconTexture);
            if (FlatTexture)
                readableIcon = FlattenTexture(readableIcon);
            Texture2D tintedIcon = ApplyColorFilter(readableIcon, IconColor);

            Graphics.DrawTexture(iconRect, tintedIcon);
        }

        // Restore the previous matrix
        GL.PopMatrix();

        // Create a new Texture2D to hold the final image
        Texture2D finalTexture = new Texture2D(size, size, TextureFormat.ARGB32, false);

        // Read the pixels from the RenderTexture into the Texture2D
        finalTexture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        finalTexture.Apply();

        // Encode the texture to PNG
        byte[] pngData = finalTexture.EncodeToPNG();

        // Save the PNG to the specified path
        File.WriteAllBytes(path, pngData);

        // Cleanup
        RenderTexture.active = null;
        renderTexture.Release();
        DestroyImmediate(finalTexture);
        DestroyImmediate(renderTexture);

        // Refresh the project to display the new png file
        AssetDatabase.Refresh();
    }



    private Vector2 AdaptiveScaling(float factor = 0.5f, bool horizontal = false, bool vertical = true)
    {
        float min1 = horizontal ? position.width : Mathf.Infinity;
        float min2 = vertical ? position.height : Mathf.Infinity;

        float scaleFactor = Mathf.Min(min1, min2) * factor; // Adjust the scaling factor as needed
        Vector2 scaledSize = new Vector2(scaleFactor, scaleFactor);

        return scaledSize;
    }

    /// <summary>
    /// The Unity default 'folderTexture' is readonly - meaning we cannot read/write pixel data
    /// We want to apply a white filter to the icon for maximal customisation
    /// Therefor to convert all of the icon's pixels white, we must clone the texture into a readable format
    /// This can be done via calling Graphics.Blit() to copy all texture data to a RenderTexture
    /// This RenderTexture can then be used as a base to make a new Texture2D that is mutable
    /// </summary>
    /// <returns></returns>
    private static Texture2D DefaultFolderTexture()
    {
        // Locate the default Unity folder icon as a texture 2D
        Texture2D folderTexture = EditorGUIUtility.FindTexture("Folder Icon");

        Texture2D result = LayerTexture(folderTexture);

        // Ensure base white color for all folder pixels
        FlattenTexture(result);

        result.Apply();

        return result;
    }

    /// <summary>
    /// Applies a color filter, adjusting all pixels of a readable texture
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="filter"></param>
    /// <returns></returns>
    private static Texture2D ApplyColorFilter(Texture2D texture, Color filter)
    {
        if (texture == null)
            return texture;

        int width = texture.width;
        int height = texture.height;

        // Get all pixels in one batch
        Color[] originalPixels = texture.GetPixels();
        Color[] finalPixels = new Color[originalPixels.Length];

        // Apply the filter
        for (int i = 0; i < originalPixels.Length; i++)
        {
            Color originalPixel = originalPixels[i];
            Color finalPixel = originalPixel * filter;
            finalPixel.a = originalPixel.a; // Retain original alpha value
            finalPixels[i] = finalPixel;
        }

        // Create the new texture and set all pixels in one batch
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(finalPixels);
        result.Apply();

        return result;
    }

    /// <summary>
    /// Clones a readonly texture into a writable clone of it
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    private static Texture2D LayerTexture(Texture2D texture)
    {
        RenderTexture previous = RenderTexture.active;

        // Create a temporary RenderTexture of the same size as the texture
        RenderTexture tmp = new RenderTexture(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.ARGB32);
        tmp.Create();

        // Clear the RenderTexture before blitting to ensure no previous texture is layered
        RenderTexture.active = tmp;
        GL.Clear(true, true, Color.clear);

        // Blit the pixels on texture to the RenderTexture
        Graphics.Blit(texture, tmp);

        // Create a new readable Texture2D to copy the pixels to it
        Texture2D result = new Texture2D(tmp.width, tmp.height, TextureFormat.ARGB32, false);
        // Copy the pixels from the RenderTexture to the new Texture
        result.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        result.name = texture.name;
        result.Apply();

        // Reset the active RenderTexture
        RenderTexture.active = previous;

        // Release the temporary RenderTexture
        tmp.Release();
        DestroyImmediate(tmp);

        return result;
    }


    /// <summary>
    /// 'Flattens' a texture, making it monochromatic
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    private static Texture2D FlattenTexture(Texture2D texture)
    {
        // Ensure base white color for all folder pixels
        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                float alpha = texture.GetPixel(x, y).a;
                Color color = new Color(1f, 1f, 1f, alpha);

                texture.SetPixel(x, y, color);
            }
        }

        return texture;
    }





    public class PreviewWindow : EditorWindow
    {
        public static PreviewWindow Window;
        public const float WindowSize = 350;

        private static Texture2D FlatIcon;

        public static void StartWindow()
        {
            Window = GetWindow<PreviewWindow>();
            Window.titleContent = new GUIContent(IconName + ".png");
            Window.minSize = new Vector2(WindowSize, WindowSize);
            Window.maxSize = new Vector2(WindowSize, WindowSize);
            Window.Show();

            FlatIcon = null;
            DefFolderTexture = DefaultFolderTexture();
        }

        private void OnGUI()
        {
            if (FlatTexture && FlatIcon == null)
            {
                Texture2D readableIcon = LayerTexture(IconTexture);
                if (FlatTexture)
                    FlatIcon = FlattenTexture(readableIcon);
            }

            DrawPreview();
        }

        private void DrawPreview()
        {
            Window.titleContent = new GUIContent(IconName + ".png");

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Define the size of the folder icon
            float size = WindowSize; // Adjust the size as needed
            Rect rect = GUILayoutUtility.GetRect(size, size);

            // Draw the folder icon
            GUI.DrawTexture(rect, FolderTexture);

            // Calculate a smaller rect for the icon texture to overlay
            float iconSize = size * 0.5f; // Icon is half the size of the folder icon
            Rect iconRect = new Rect(
                rect.x + IconOffset.x + (size - iconSize) / 2,
                rect.y + IconOffset.y + (size - iconSize) / 2,
                iconSize * IconScale.x,
                iconSize * IconScale.y
            );

            if (IconTexture != null)
            {
                // Draw the icon texture over the folder icon
                Texture2D readableIcon = LayerTexture(IconTexture);
                if (FlatTexture)
                    readableIcon = FlatIcon;
                Texture2D tintedIcon = ApplyColorFilter(readableIcon, IconColor);
                GUI.DrawTexture(iconRect, tintedIcon);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
    }
}
