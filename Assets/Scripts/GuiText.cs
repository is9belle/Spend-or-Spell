using UnityEngine;

public static class GuiText
{
    // sets the text color for every state (normal, hover, pressed, focused)
    public static void SetColor(GUIStyle st, Color c)
    {
        st.normal.textColor = c;
        st.hover.textColor = c;
        st.active.textColor = c;
        st.focused.textColor = c;
        st.onNormal.textColor = c;
        st.onHover.textColor = c;
        st.onActive.textColor = c;
        st.onFocused.textColor = c;
    }
}