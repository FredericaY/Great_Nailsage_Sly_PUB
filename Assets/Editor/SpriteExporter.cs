using UnityEngine;
using UnityEditor;
using System.IO;

public class SpriteExporter
{
    [MenuItem("Tools/Export Selected Sprites")]
    static void ExportSelectedSprites()
    {
        Object[] selection = Selection.objects;

        foreach (Object obj in selection)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    ExportSprite(sprite, path);
                }
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("Sprite export finished.");
    }

    static void ExportSprite(Sprite sprite, string sourcePath)
    {
        Texture2D sourceTex = sprite.texture;
        Rect rect = sprite.rect;

        Texture2D newTex = new Texture2D(
            (int)rect.width,
            (int)rect.height,
            TextureFormat.RGBA32,
            false
        );

        Color[] pixels = sourceTex.GetPixels(
            (int)rect.x,
            (int)rect.y,
            (int)rect.width,
            (int)rect.height
        );

        newTex.SetPixels(pixels);
        newTex.Apply();

        string dir = Path.GetDirectoryName(sourcePath) + "/ExtractedSprites";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string filePath = $"{dir}/{sprite.name}.png";
        File.WriteAllBytes(filePath, newTex.EncodeToPNG());
    }
}
