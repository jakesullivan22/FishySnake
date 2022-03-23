using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum layers { default0, transparentFX, ignoreRaycast, blank3, water, UI, blank6, blank7, obstacle, pickup, edibleTail};
    public enum moveMode { grid, free, stop, transition, terminator}
    public static GameManager gm;
    public delegate void VoidNull();
    public delegate void VoidFish(Fish _fish);
    public delegate void VoidFloat(float _float);
    public delegate void VoidVector4(Vector4 _vector4);
    public delegate void VoidMoveMode(moveMode _moveMode);
    public GameObject lightningPrefab;

    private void Awake()
    {
        if (gm == null)
        {
            gm = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (gm != this)
        {
            Destroy(gameObject);
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void RotateRect(RectTransform _rect, Vector2 _direction)
    {
        float angle = Vector2.SignedAngle(Vector2.right, _direction);
        _rect.rotation = Quaternion.Euler(new Vector3(0f, 0f, angle)); //rotate the head
    }
}
