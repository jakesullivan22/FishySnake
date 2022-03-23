using System.Collections;
using UnityEngine;

public class Background : MonoBehaviour
{
    public GameObject playArea, leftWall, rightWall, topWall, bottomWall;
    readonly int width = 60;
    readonly int height = 30;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Setup());
    }

    IEnumerator Setup()
    {
        PlayerController pc = PlayerController.pc;
        yield return new WaitUntil(() => pc.GetTileDiameter() > 0);
        float tileRadius = pc.GetTileDiameter();

        //play area size adjust
        RectTransform playAreaRect = playArea.GetComponent<RectTransform>();
        float playAreaWidth = Mathf.Min(width, Mathf.FloorToInt(playAreaRect.rect.width / tileRadius)) * tileRadius;
        float playAreaHeight = Mathf.Min(height, Mathf.FloorToInt(playAreaRect.rect.height / tileRadius)) * tileRadius;
        float sideBorderWidth = (Screen.width - playAreaWidth) / 2f;
        float topAndBottomBorderHeight = (Screen.height - playAreaHeight) / 2f;
        playAreaRect.offsetMin = new Vector2(sideBorderWidth, topAndBottomBorderHeight);
        playAreaRect.offsetMax = new Vector2(-sideBorderWidth, -topAndBottomBorderHeight);

        //left wall size adjust
        RectTransform leftWallRect = leftWall.GetComponent<RectTransform>();
        leftWallRect.offsetMax = new Vector2(sideBorderWidth, 0f);
        BoxCollider2D leftCollider = leftWall.GetComponent<BoxCollider2D>();
        leftCollider.size = new Vector2(sideBorderWidth, Screen.height);
        leftCollider.offset = new Vector2(sideBorderWidth / 2f, 0f);

        //right wall size adjust
        RectTransform rightWallRect = rightWall.GetComponent<RectTransform>();
        rightWallRect.offsetMin = new Vector2(-sideBorderWidth, 0f);
        BoxCollider2D rightCollider = rightWall.GetComponent<BoxCollider2D>();
        rightCollider.size = new Vector2(sideBorderWidth, Screen.height);
        rightCollider.offset = new Vector2(-sideBorderWidth / 2f, 0f);

        //bottom wall size adjust
        RectTransform bottomWallRect = bottomWall.GetComponent<RectTransform>();
        bottomWallRect.offsetMin = new Vector2(sideBorderWidth, 0f);
        bottomWallRect.offsetMax = new Vector2(-sideBorderWidth, topAndBottomBorderHeight);
        BoxCollider2D bottomCollider = bottomWall.GetComponent<BoxCollider2D>();
        bottomCollider.size = new Vector2(Screen.width - sideBorderWidth * 2f, topAndBottomBorderHeight);
        bottomCollider.offset = new Vector2(0f, topAndBottomBorderHeight / 2f);

        //top wall size adjust
        RectTransform topWallRect = topWall.GetComponent<RectTransform>();
        topWallRect.offsetMin = new Vector2(sideBorderWidth, -topAndBottomBorderHeight);
        topWallRect.offsetMax = new Vector2(-sideBorderWidth, 0f);
        BoxCollider2D topCollider = topWall.GetComponent<BoxCollider2D>();
        topCollider.size = new Vector2(Screen.width - sideBorderWidth * 2f, topAndBottomBorderHeight);
        topCollider.offset = new Vector2(0f, -topAndBottomBorderHeight / 2f);
    }
}
