﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Croupier : MonoBehaviour
{
    [Header("Packet creation")]
    public TextAsset vocabularyFile;
    public TextAsset goodHeadersFile;
    public TextAsset badHeadersFile;

    public float packetsPerSecond;
    public float creationPeriod = 0.05f;
    public GameObject linePrefab;

    [Header("Bufer handling")]
    public float verticalPadding = 0;
    public float relativeVerticalPadding = 0.1f;
    public float screenCrossingTime = 3f;

    [Header("Typing")]
    public InputField field;

    RectTransform rt;
    Line[] lines;
    string[] gHeaders, vocabulary, bHeaders;
    private string[] ParseTextAsset(TextAsset textAsset)
    {
        return (from s in textAsset.text.Split('\n', '\r', ' ') where !string.IsNullOrEmpty(s) select s).ToArray();
    }
    void Start()
    {
        rt = transform as RectTransform;
        Debug.Assert(rt != null);
        var lrt = linePrefab.transform as RectTransform;
        var padding = (verticalPadding + relativeVerticalPadding * rt.rect.height);

        int linesCount = Mathf.FloorToInt((rt.rect.height - padding * 2) / lrt.rect.height);
        lines = new Line[linesCount];

        Packet.maxSpeed = rt.rect.width / screenCrossingTime;
        for (int i = 0; i < lines.Length; i++)
        {
            var l = Instantiate(linePrefab);
            lines[i] = l.GetComponent<Line>();
            lrt = l.transform as RectTransform;
            lrt.SetParent(transform);
            lrt.localScale = Vector3.one;
            var w = lrt.rect.width;
            var h = lrt.rect.height;
            lrt.anchorMin = Vector2.one;
            lrt.anchorMax = Vector2.one;
            lrt.anchoredPosition = new Vector2(-w * 0.5f, -padding - h * 0.5f - h * i);
            lines[i].OnPacketClear += OnPacketClear;
            lines[i].OnPacketDrop += OnPacketDrop;
        }
        vocabulary = ParseTextAsset(vocabularyFile);
        gHeaders = ParseTextAsset(goodHeadersFile);
        bHeaders = ParseTextAsset(badHeadersFile);
    }
    private void OnPacketClear(Packet p)
    {
        var data = p.data;
        if (gHeaders.Contains(data.header))
        {
            //TODO accelerate
        }
        else if (bHeaders.Contains(data.header))
        {
            //TODO decelerate
        }
        else Debug.LogError($"Packet header {data.header} is not contained");
    }
    private void OnPacketDrop(Packet p)
    {
        var data = p.data;
        if (gHeaders.Contains(data.header))
        {
            //TODO decelerate
        }
        else if (bHeaders.Contains(data.header))
        {
            //TODO accelerate
        }
        else Debug.LogError($"Packet header {data.header} is not contained");
    }

    private float nextCreationTime = 0f;
    private void CreatePackets()
    {
        if (Time.time < nextCreationTime)
            return;
        var freeLines = (from l in lines where l != null && !l.busy select l).ToList();
        if (freeLines.Count == 0) return;

        var probability = packetsPerSecond * creationPeriod;
        nextCreationTime = Time.time + creationPeriod;
        if (Random.value <= probability)
        {
            var lineInx = Mathf.FloorToInt(Random.Range(0f, freeLines.Count));
            var headerInx = Mathf.FloorToInt(Random.Range(0f, gHeaders.Length + bHeaders.Length));
            var wordInx = Mathf.FloorToInt(Random.Range(0f, vocabulary.Length));
            bool goodPacket = headerInx < gHeaders.Length;
            string h = !goodPacket ? bHeaders[headerInx % bHeaders.Length] : gHeaders[headerInx];

            freeLines[lineInx].CreatePacket(new Packet.Data(h, vocabulary[wordInx], goodPacket));
        }
    }
    void Update()
    {
        CreatePackets();

        if (Input.anyKeyDown && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != field.gameObject)
            field.Select();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var s = field.text.Trim();
            var occurences = 0;
            foreach (var line in lines)
            {
                occurences += line.ClearPackets(s);
            }
            field.text = "";
            field.Select();
        }
    }
}
