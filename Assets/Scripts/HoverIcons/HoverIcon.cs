using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HoverIcon : MonoBehaviour
{

    public List<Sprite> icons = new();
    public Image image;
    public Transform followedTransform;
    public Vector3 overhead;

    private Camera cameraObject;

    private Image background;

    public void ChangeIcon(int id)
    {
        image.sprite = icons[id];
    }

    public void ChangeRed(float val)
    {
        background.color = Color.Lerp(Color.white, Color.red, val);
    }

    public void ChangeIconVisibility(bool visible)
    {
        image.enabled = visible;
        background.enabled = visible;
        transform.GetComponent<Image>().enabled = visible;
    }

    private void Start()
    {
        image = transform.GetChild(0).GetChild(0).GetComponent<Image>();
        cameraObject = GameObject.Find("Main Camera").GetComponent<Camera>();
        background = transform.GetChild(0).GetComponent<Image>();
    }

    void Update()
    {
        transform.position = followedTransform.position + overhead;
    }
}
