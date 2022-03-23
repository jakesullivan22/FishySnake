using UnityEngine;
using UnityEngine.UI;

public class Lightning : MonoBehaviour
{
    public Sprite[] sprites;
    public float timeUntilNextSpriteSwitch;
    float timer;
    int spriteIndex;
    Image myImage;

    private void Start()
    {
        myImage = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer > timeUntilNextSpriteSwitch)
        {
            spriteIndex++;
            timer = 0f;
            if (spriteIndex >= sprites.Length)
            {
                Destroy(gameObject);
                return;
            }

            myImage.sprite = sprites[spriteIndex];
        }
    }
}
