using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScrollableContent : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
{
    public enum ScrollDirection
    {
        VerticalDesc,
        VerticalAsc,
        Horizontal
    }

    public ScrollDirection scrollDirection = ScrollDirection.VerticalDesc;

    RectTransform rectTransform;
    [SerializeField]
    private float startPos;

    [SerializeField]
    private float HardClampDistance = 20;

    private bool dragging = false;

    [SerializeField]
    private float maxPos;

    public float maxPosOffset = 0;

    public bool updateChildrenPositions = false;

    private void Start()
    {
        rectTransform = transform.GetChild(0).GetComponent<RectTransform>();
        if (scrollDirection == ScrollDirection.VerticalDesc)
            startPos = rectTransform.position.y;
        else if (scrollDirection == ScrollDirection.VerticalAsc)
            startPos = rectTransform.position.y;
        else if (scrollDirection == ScrollDirection.Horizontal)
            startPos = rectTransform.position.x;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (scrollDirection == ScrollDirection.VerticalDesc)
        {
            Vector3 pos = rectTransform.position;
            float deltaY = eventData.delta.y;
            // make dragging exponentially harder when reaching the soft clamp distance
            if (pos.y < startPos)
            {
                deltaY *= Mathf.Pow(1 - (startPos - pos.y) / HardClampDistance, 1);
            }
            else if (pos.y > maxPos)
            {
                deltaY *= Mathf.Pow(1 - (pos.y - maxPos) / HardClampDistance, 1);
            }

            rectTransform.position += new Vector3(0, deltaY, 0);
        }
        else if (scrollDirection == ScrollDirection.VerticalAsc)
        {
            Vector3 pos = rectTransform.position;
            float deltaY = eventData.delta.y;
            // make dragging exponentially harder when reaching the soft clamp distance
            if (pos.y < startPos)
            {
                deltaY *= Mathf.Pow(1 - (pos.y - startPos) / HardClampDistance, 1);
            }
            else if (pos.y > maxPos)
            {
                deltaY *= Mathf.Pow(1 - (maxPos - pos.y) / HardClampDistance, 1);
            }

            rectTransform.position += new Vector3(0, deltaY, 0);
        }
        else if (scrollDirection == ScrollDirection.Horizontal)
        {
            Vector3 pos = rectTransform.position;
            float deltaX = eventData.delta.x;

            if (pos.x > startPos)
            {
                deltaX *= Mathf.Pow(1 - (pos.x - startPos) / HardClampDistance, 1);
            }
            else if (pos.x < maxPos)
            {
                deltaX *= Mathf.Pow(1 - (maxPos - pos.x) / HardClampDistance, 1);
            }

            Debug.Log(deltaX);


            rectTransform.position += new Vector3(deltaX, 0, 0);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
    }

    private void ClampPosition()
    {
        Vector3 pos = rectTransform.position;

        // compute max Y based on the number of children and their height
        if (rectTransform.childCount == 0)
        {
            return;
        }
        if (scrollDirection == ScrollDirection.VerticalDesc)
        {
            float height = 0;
            foreach (RectTransform child in rectTransform)
            {
                height += child.rect.height * GameManager.canvas.transform.localScale.y;
            }
            maxPos = startPos + height + maxPosOffset * GameManager.canvas.transform.localScale.y;
            maxPos = Mathf.Max(maxPos, startPos);

            // Debug.Log(maxY);

            pos.y = Mathf.Clamp(pos.y, startPos - HardClampDistance, maxPos + HardClampDistance);

            // apply a soft clamp that slowly moves the content back to the soft clamp position
            if (!dragging && pos.y < startPos)
            {
                pos.y += (startPos - pos.y) / 10;
            }
            else if (!dragging && pos.y > maxPos)
            {
                pos.y -= (pos.y - maxPos) / 10;
            }
        }
        else if (scrollDirection == ScrollDirection.VerticalAsc)
        {
            float height = 0;
            foreach (RectTransform child in rectTransform)
            {
                height += child.rect.height * GameManager.canvas.transform.localScale.y;
            }
            maxPos = startPos - height + maxPosOffset * GameManager.canvas.transform.localScale.x;
            maxPos = Mathf.Min(maxPos, startPos);

            pos.y = Mathf.Clamp(pos.y, maxPos - HardClampDistance, startPos + HardClampDistance);

            if (!dragging && pos.y < maxPos)
            {
                pos.y += (maxPos - pos.y) / 10;
            }
            else if (!dragging && pos.y > startPos)
            {
                pos.y -= (pos.y - startPos) / 10;
            }
        }
        else if (scrollDirection == ScrollDirection.Horizontal)
        {
            float width = 0;
            foreach (RectTransform child in rectTransform)
            {
                width += child.rect.width * GameManager.canvas.transform.localScale.x;
            }

            maxPos = startPos - width + maxPosOffset * GameManager.canvas.transform.localScale.x;
            maxPos = Mathf.Min(maxPos, startPos);

            pos.x = Mathf.Clamp(pos.x, maxPos - HardClampDistance, startPos + HardClampDistance);

            if (!dragging && pos.x < maxPos)
            {
                pos.x += (maxPos - pos.x) / 10;
            }
            else if (!dragging && pos.x > startPos)
            {
                pos.x -= (pos.x - startPos) / 10;
            }
        }

        rectTransform.position = pos;
    }

    public void UpdateChildenPositions()
    {
        if (scrollDirection == ScrollDirection.VerticalDesc)
        {
            rectTransform.position = new Vector3(rectTransform.position.x, startPos, rectTransform.position.z);

            float cumHeight = startPos * 2;
            foreach (RectTransform child in rectTransform)
            {
                child.position = new Vector3(child.position.x, cumHeight, child.position.z);
                cumHeight -= child.rect.height;
            }
        }
        else if (scrollDirection == ScrollDirection.VerticalAsc)
        {
            rectTransform.position = new Vector3(rectTransform.position.x, startPos, rectTransform.position.z);

            float cumHeight = startPos;
            foreach (RectTransform child in rectTransform)
            {
                child.position = new Vector3(child.position.x, cumHeight, child.position.z);
                cumHeight += child.rect.height;
            }
        }
        else if (scrollDirection == ScrollDirection.Horizontal)
        {
            rectTransform.position = new Vector3(startPos, rectTransform.position.y, rectTransform.position.z);

            float cumWidth = startPos;
            foreach (RectTransform child in rectTransform)
            {
                child.position = new Vector3(cumWidth, child.position.y, child.position.z);
                cumWidth += child.rect.width;
            }
        }

    }

    private void Update()
    {
        ClampPosition();

        if (updateChildrenPositions)
        {
            UpdateChildenPositions();
            updateChildrenPositions = false;
        }
    }
}
