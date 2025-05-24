using UnityEngine;
public static class CursorManager
{
    public static Texture2D defaultCursor;
    public static Texture2D interactCursor; // Pointing hand
    public static Texture2D dialogueCursor; // Speech bubble
    // public static Vector2 defaultHotspot = Vector2.zero;
    // public static Vector2 interactHotspot = new Vector2(x,y); // Set appropriately

    public static void SetDefault() { Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto); }
    public static void SetInteract() { Cursor.SetCursor(interactCursor, new Vector2(4, 0) /*example hotspot*/, CursorMode.Auto); }
    public static void SetDialogue() { Cursor.SetCursor(dialogueCursor, Vector2.zero, CursorMode.Auto); }
}