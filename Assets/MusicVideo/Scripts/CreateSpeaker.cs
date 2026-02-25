// UMD IMDM290 
// Instructor: Myungin Lee
// This tutorial introduce a way to draw shapes and align them in a circle with colors.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateSpeaker : MonoBehaviour
{

    [Header("Visuals")]

    public GameObject prefab;
    public GameObject parent;
    public int colorHue;
    public int size = 25; // Size of the grid (number of shapes in one direction)

    int rowCount, colCount;
    Vector3[][] floatPositions, speakerPositions, expandTargets,scaleA, scaleB;
    GameObject[][] shapes;
    Renderer[][] shapesRenderers;
    Quaternion[][] rotationA;
    Color[][][] colors;

    Quaternion targetRotation;
    System.Random rand = new System.Random();

    bool positionsSet = false;
    bool speakerStart = false, speakerFormed = false, speakerFormed2 = false;
    bool expanded = false, expanded2 = false;   
    float expandStartTime = 0f, speakerStartTime = 0f;

    [Header("Glow Trail")]
    public Material trailMaterial;
    public Material shapeMaterial;
    public float trailTime = 1.2f;

    [Header("Audio")]
    // Global visual controls for all three plots.
    public float scale = 10f;
    public int displayBins = 192;
    public float plotWidth = 16f;
    public float rowGap = 2.8f;
    public Vector3 origin = new Vector3(-8f, 4f, 0f);
    // Lowest frequency used for the log-scale mapping.
    public float minLogHz = 20f;
    // Number of representative x positions to annotate.
    public int labelCount = 8;
    // Negative value moves labels below the x-axis.
    public float labelYOffset = -0.45f;
    private const int RowCount = 3;
    private const int LinearRow = 0;
    private const int LogRow = 1;
    private const int MelRow = 2;

    // Row index order must match rowNames and rowColors.
    private readonly string[] rowNames = { "1) Linear", "2) Log", "3) Mel" };
    private readonly Color[] rowColors =
    {
        new Color(0.2f, 0.8f, 1.0f, 1f),
        new Color(0.2f, 1.0f, 0.45f, 1f),
        new Color(1.0f, 0.65f, 0.2f, 1f)
    };

    GameObject[,] bars;
    TextMesh[,] labels;
    int[,] mappedBins;
    float[,] mappedHz;
    int[] labelDisplayIndices;
    Font font;

    int fftSize;
    float hzPerBin;
    float nyquist;
    float xStep;

    public float time = 0f;

    // Start is called before the first frame update
    void Start() 
    {
        time = 0.0f;
        targetRotation = Quaternion.identity;
        expandStartTime = time;
        InitShapes();
        SetupAudioReactivity();
    }    

    void Update() 
    {
        time = Time.time;

        if (!speakerFormed) {
            AudioReactive();
            if(!expanded) {
                if (time >= 2.75f){
                    Expand(2.75f, .147f);
                } 
            } else {
                if (speakerStart) {
                    speakerStartTime = time;
                    speakerStart = false;
                }
                FormSpeaker(speakerStartTime,.21f);
            }
        } else if(speakerFormed) {
            if (time < 33f) {
                if (time < 28.8f )
                {
                    AnimateSpeaker();
                }
                if (time >= 28.8f )
                {
                    Expand2(28.8f,.15f);
                }
            } else {
                if (!speakerFormed2)
                {
                    FormSpeaker(33f,1f);
                } else {
                    AnimateSpeaker();
                }
            }
        }
    }

    void InitShapes()
    {
        rowCount = 2 * size + 1;
        colCount = (int) ((3.25f) * size + 1);
   
        shapes = new GameObject[rowCount][];
        shapesRenderers = new Renderer[rowCount][];
        speakerPositions = new Vector3[rowCount][];
        colors = new Color[rowCount][][];

        floatPositions = new Vector3[rowCount][];
        rotationA = new Quaternion[rowCount][];
        scaleA = new Vector3[rowCount][];
        scaleB = new Vector3[rowCount][];

        expandTargets = new Vector3[rowCount][];

        for(int row = 0; row < rowCount; row++)
        {
            shapes[row] = new GameObject[colCount];
            shapesRenderers[row] = new Renderer[colCount];
            speakerPositions[row] = new Vector3[colCount];
            colors[row] = new Color[colCount][];

            floatPositions[row] = new Vector3[colCount];
            rotationA[row] = new Quaternion[colCount];
            scaleA[row] = new Vector3[colCount];
            scaleB[row] = new Vector3[colCount];
            expandTargets[row]= new Vector3[colCount];

            for (int col = 0; col < colCount; col++)
            {    
                shapes[row][col] = GameObject.Instantiate(prefab);

                colors[row][col] = new Color[4];
                colors[row][col][0] = new Color((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
                colors[row][col][1] = new Color((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble() , (float)rand.NextDouble());
                colors[row][col][2] = new Color((float)rand.NextDouble(), (float)rand.NextDouble() , (float)rand.NextDouble() ,(float)rand.NextDouble());
                colors[row][col][3] = new Color((float)rand.NextDouble() , (float)rand.NextDouble() , (float)rand.NextDouble() , (float)rand.NextDouble());

                shapes[row][col].transform.position = new Vector3(5f*((colors[row][col][0]).r - 0.5f) , 5f*((colors[row][col][0]).g  - 0.5f), 5f*((colors[row][col][0]).b - 0.5f));
                shapes[row][col].transform.rotation = Quaternion.Euler(720 * ((colors[row][col][1]).r) - 360, 720 * ((colors[row][col][1]).g) - 360, (720 * ((colors[row][col][1]).b) - 360));
                shapes[row][col].transform.localScale = new Vector3((0.75f * (colors[row][col][3].r + 0.5f)), (0.75f*(colors[row][col][3].g + 0.2f)), (0.75f*(colors[row][col][3]).b + .5f));
               
                int x = col - (colCount / 2); // Center the shapes around (0, 0)
                int y = row - (rowCount / 2); // Center the shapes around (0, 0)
                float radius = Mathf.Sqrt(x * x + y * y);   // Fun fact Pythagorous wasnt even the first to discover this thats just white supremacy type.
                float colorHue = 1*(Mathf.Sin(radius * (2*Mathf.PI / ((float)size*(1.2f))) - (Mathf.PI/2)) + 1) +1f;
                colors[row][col][2] = Color.HSVToRGB(colorHue, 100f, colorHue); // Full saturation and brightness

                shapes[row][col].transform.parent = parent.transform;
                shapesRenderers[row][col] = shapes[row][col].GetComponent<Renderer>();
                shapesRenderers[row][col].material = shapeMaterial;
                shapesRenderers[row][col].material.color = colors[row][col][2];
            }
        }
    }
    
    void Expand(float expandStartTime , float speed) {      
        float lerpFraction = Mathf.Clamp01((time - expandStartTime) * speed);

        for(int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < colCount; col++)
            {  
                if (positionsSet == false) {

                    floatPositions[row][col] = shapes[row][col].transform.position;
                    expandTargets[row][col] = new Vector3(50f * Random.Range(-1f, 1f), 50f * Random.Range(-1f, 1f), 50f * Random.Range(-1f, 1f));
                } 
                shapes[row][col].transform.position = Vector3.Lerp(floatPositions[row][col], expandTargets[row][col], lerpFraction);
            }
        }
        positionsSet = true; 
        if (lerpFraction >= .95f) {
            if (expanded == false) {
                  expanded = true;
                  positionsSet = false;
                  speakerStart = true;
            }
        } 
    }  


    void Expand2(float expandStartTime , float speed ) {
        float lerpFraction = .5f * Mathf.Sin((time - expandStartTime)*speed) + .5f;        
        for(int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < colCount; col++)
            {  
                if (positionsSet == false) {
                    floatPositions[row][col] = shapes[row][col].transform.position;
                    expandTargets[row][col] = new Vector3(50f * Random.Range(-1f, 1f), 50f * Random.Range(-1f, 1f), 50f * Random.Range(-1f, 1f));
                } 
                shapes[row][col].transform.position = Vector3.Lerp(floatPositions[row][col], expandTargets[row][col], lerpFraction);
            }
        }
        positionsSet = true; 
        if (lerpFraction < 0) {
            if (expanded2 == false) {
                  expanded2 = true;
                  positionsSet = false;
            }
        } 
    }  


    void AudioReactive(){
        for (int row =0; row < rowCount; row++){
            for (int col = 0; col < colCount; col++){

            float scale = 1f + AudioSpectrum.audioAmp;
            shapes[row][col].transform.localScale = new Vector3(scale, shapes[row][col].transform.localScale.y, shapes[row][col].transform.localScale.z);
            shapes[row][col].transform.rotation *= Quaternion.Euler(AudioSpectrum.audioAmp, 1f, 1f);

            }
        }
    }


   void FormSpeaker(float toSpeakerTime, float speed)
   {
        float lerpFraction = .5f * Mathf.Sin((time - toSpeakerTime) * speed ) + .5f;        
        // float lerpFraction = Mathf.Clamp01((Time.time - toSpeakerTime) );
        for(int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < colCount; col++)
            {  
                if (positionsSet == false) {
                    floatPositions[row][col] = shapes[row][col].transform.position;
                    rotationA[row][col] = shapes[row][col].transform.rotation;

                    int x = col - (colCount / 2); // Center the shapes around (0, 0)
                    int y = row - (rowCount / 2); // Center the shapes around (0, 0)
                    
                    speakerPositions[row][col] = new Vector3(x, y, 10f);

                    float radius = Mathf.Sqrt(x * x + y * y);   // Fun fact Pythagorous wasnt even the first to discover this thats just white supremacy type.
                    float scaleOfShape = 4f*(Mathf.Sin(radius * (2*Mathf.PI / ((float)size*(1.2f))) - (Mathf.PI/2)) + 1) + 1.5f; // Scale shapes based on radius (optional)

                    scaleA[row][col] = shapes[row][col].transform.localScale;
                    scaleB[row][col] = new Vector3(scaleOfShape * .3f,scaleOfShape * .3f,.1f); 
                } 
                shapes[row][col].transform.localScale = Vector3.Lerp(scaleA[row][col], scaleB[row][col], lerpFraction);
                shapes[row][col].transform.position = Vector3.Lerp(floatPositions[row][col], speakerPositions[row][col], lerpFraction);
                shapes[row][col].transform.rotation = Quaternion.Slerp(rotationA[row][col], targetRotation, lerpFraction);
            }
        }
        positionsSet = true; 
        if (lerpFraction >= .99f) {
            if (!speakerFormed) {
                speakerFormed = true;
                positionsSet = false;
            } else if (!speakerFormed2) {
                speakerFormed2 = true;
                positionsSet = false;
            }
        } 
    }

    void AnimateSpeaker() 
    {
        for(int row = 0; row < rowCount; row++) 
        {
            for(int col = 0; col < colCount; col++) 
            {
                int x = col - (colCount / 2); // Center the shapes around (0, 0)
                int y = row - (rowCount / 2); // Center the shapes around (0, 0)

                float radius = Mathf.Sqrt(x * x + y * y); 
                float z = 10f;
               
                if (AudioSpectrum.samples != null) 
                {
                    int bin = ((int)radius > displayBins - 1 )? 0 : (displayBins - 1) - (int)radius;                   
                    int fftBin = mappedBins[MelRow, bin];
                    float amp = Mathf.Max(AudioSpectrum.samples[fftBin] * scale * scale, 0.001f);
                    z = 10f - (amp); // Move shape based on amplitude (optional)
                } 

                shapes[row][col].transform.position = new Vector3(x, y, z); // Move shape based on amplitude (optional)


                float scaleOfRotation = 15*(Mathf.Sin(time + radius * (2*Mathf.PI / ((float)size*(1.2f))) - (Mathf.PI/2)) + 1) + 1.5f; // Scale shapes based on radius (optional)
                shapes[row][col].transform.rotation = Quaternion.Euler(time * scaleOfRotation, 0, Time.time * scaleOfRotation); // Rotate shapes over time
                float colorHue = 0.0f;
                
                if (time < 75.5f)
                {
                    if(!speakerFormed2) {
                        colorHue = (radius/size) * Mathf.Sin( Mathf.PI/2 * Time.time ) + .05f;
                    } else {
                        colorHue = 1*(Mathf.Sin(Time.time + radius * (2*Mathf.PI / ((float)size*(1.2f))) - (Mathf.PI/2)) + 1) +1f;
                    }

                    colors[row][col][2] = Color.HSVToRGB(colorHue, 100f, colorHue); // Full saturation and brightness
                    shapesRenderers[row][col].material.color = colors[row][col][2];
                }
            }
        }
    }


    // void FloatAround(float speed)
    // {
    //     for(int row = 0; row < rowCount; row++)
    //     {
    //         for (int col = 0; col < colCount; col++)
    //         {   
    //             Color.RGBToHSV(colors[row][col][0], out float h, out float s, out float v);
    //             Color.RGBToHSV(colors[row][col][1], out float h2, out float s2, out float v2);
    //             Color.RGBToHSV(colors[row][col][2], out float h3, out float s3, out float v3);
    //             Color.RGBToHSV(colors[row][col][3], out float h4, out float s4, out float v4);

    //             float radius = Mathf.Sqrt(Mathf.Pow(shapes[row][col].transform.position.x, 2) + Mathf.Pow(shapes[row][col].transform.position.y, 2)); 
    
    //             float amp = 1f;
    //             if (AudioSpectrum.samples != null) 
    //             {
    //                 int bin = ((int)radius > displayBins - 1 )? 0 : (displayBins - 1) - (int)radius;                   
    //                 int fftBin = mappedBins[MelRow, bin];
    //                 amp = Mathf.Max(AudioSpectrum.samples[fftBin] * scale * scale, 0.001f);
    //             } 
    //             //  AudioSpectrum.audioAmp
    //             //  speed

    //             float newH1 = (h + Time.deltaTime  * speed * amp) % 1f;
    //             float newH2 = (h2 + Time.deltaTime * speed) % 1f;
    //             float newH3 = (h3 + Time.deltaTime * speed) % 1f;
    //             float newH4 = (h4 + Time.deltaTime * speed) % 1f;

    //             colors[row][col][0] = Color.HSVToRGB(newH1, s, v);
    //             colors[row][col][1] = Color.HSVToRGB(newH2, s2, v2);
    //             // colors[row][col][2] = Color.HSVToRGB(newH3, s3, v3);
    //             colors[row][col][3] = Color.HSVToRGB(newH4, s4, v4);

    //             // shapes[row][col]

    //             shapes[row][col].transform.position = new Vector3(50f*((colors[row][col][0]).r - 0.5f) , 25f*((colors[row][col][0]).g  - 0.5f), 50f*((colors[row][col][0]).b - 0.5f));
    //             shapes[row][col].transform.rotation = Quaternion.Euler(720 * ((colors[row][col][1]).r) - 360, 720 * ((colors[row][col][1]).g) - 360, (720 * ((colors[row][col][1]).b) - 360 ));
    //             // shapes[row][col].transform.localScale = new Vector3((0.2f * (colors[row][col][3].r + 0.5f)), (0.2f*(colors[row][col][3].g + 0.2f)), (0.5f*(colors[row][col][3]).b + .5f));
    //             shapesRenderers[row][col].material.color = colors[row][col][2];
    //         }
    //     }
    // }

      void SetupAudioReactivity()
    {
        // FFT settings come from AudioSpectrum (source of spectrum data).
        fftSize = AudioSpectrum.FFTSIZE;
        // Nyquist is the maximum analyzable frequency in digital audio.
        nyquist = AudioSettings.outputSampleRate * 0.5f;
        hzPerBin = nyquist / fftSize;
        displayBins =  (int) (size * 2);

        // Keep settings in a practical range for the classroom demo.
        displayBins = Mathf.Clamp(displayBins, 16, fftSize);
        labelCount = Mathf.Clamp(labelCount, 2, displayBins);
        xStep = plotWidth / (displayBins - 1);
        
        // Built-in font so students can run without extra assets.
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bars = new GameObject[RowCount, displayBins];
        labels = new TextMesh[RowCount, labelCount];
        mappedBins = new int[RowCount, displayBins];
        mappedHz = new float[RowCount, displayBins];
        labelDisplayIndices = BuildRepresentativeIndices(labelCount, displayBins);

        // 1) Map display x positions to FFT bins for each scale.
        BuildMappings();
        // 2) Create visuals once, then only update transforms each frame.
        // CreateBars();
        // CreateRowTitles();
        // CreateLabels();  
    }


    void BuildMappings()
    {
        // Build each row separately so students can read one scale at a time.
        BuildLinearMapping();
        BuildLogMapping();
        BuildMelMapping();
    }

    void BuildLinearMapping()
    {
        for (int i = 0; i < displayBins; i++)
        {
            // t is x position in [0, 1].
            float t = NormalizedX(i);
            float hz = t * nyquist;
            SetMapping(LinearRow, i, hz);
        }
    }

    void BuildLogMapping()
    {
        float safeMinHz = Mathf.Clamp(minLogHz, hzPerBin, nyquist);

        for (int i = 0; i < displayBins; i++)
        {
            float t = NormalizedX(i);
            float hz = 0f;

            if (t > 0f)
            {
                // Convert [0,1] -> log-frequency range.
                hz = Log01ToHz(t, safeMinHz, nyquist);
            }

            SetMapping(LogRow, i, hz);
        }
    }

    void BuildMelMapping()
    {
        float melMax = HzToMel(nyquist);

        for (int i = 0; i < displayBins; i++)
        {
            float t = NormalizedX(i);
            float hz = 0f;

            if (t > 0f)
            {
                // Interpolate in mel space, then convert mel -> Hz.
                float melValue = Mathf.Lerp(0f, melMax, t);
                hz = MelToHz(melValue);
            }
            SetMapping(MelRow, i, hz);
        }
    }

    float NormalizedX(int index)
    {
        return index / (float)(displayBins - 1);
    }

    float Log01ToHz(float t, float minHz, float maxHz)
    {
        float minLog10 = Mathf.Log10(minHz);
        float maxLog10 = Mathf.Log10(maxHz);
        float logValue = Mathf.Lerp(minLog10, maxLog10, t);
        return Mathf.Pow(10f, logValue);
    }

    void SetMapping(int row, int displayIndex, float hz)
    {
        // Convert continuous frequency (Hz) to a discrete FFT bin index.
        int fftBin = Mathf.Clamp(Mathf.RoundToInt(hz / hzPerBin), 0, fftSize - 1);
        mappedBins[row, displayIndex] = fftBin;
        // Store quantized frequency (actual bin center used in drawing).
        mappedHz[row, displayIndex] = fftBin * hzPerBin;
    }

    // void CreateBars()
    // {
    //     // Row 0: Linear, Row 1: Log, Row 2: Mel.
    //     for (int row = 0; row < RowCount; row++)
    //     {
    //         for (int i = 0; i < displayBins; i++)
    //         {
    //             GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //             bar.name = $"V2_{row}_{i}";
    //             bar.transform.SetParent(transform);
    //             bar.transform.position = GetBasePosition(row, i);
    //             bar.transform.localScale = new Vector3(0.08f, 0.001f, 0.08f);
    //             bar.GetComponent<Renderer>().material.color = rowColors[row];
    //             bars[row, i] = bar;
    //         }
    //     }
    // }

    // void CreateRowTitles()
    // {
    //     for (int row = 0; row < RowCount; row++)
    //     {
    //         GameObject go = new GameObject($"Title_{row}");
    //         go.transform.SetParent(transform);
    //         go.transform.position = GetBasePosition(row, 0) + new Vector3(-1.1f, 0.1f, 0f);

    //         TextMesh tm = go.AddComponent<TextMesh>();
    //         tm.font = font;
    //         tm.text = rowNames[row];
    //         tm.fontSize = 100;
    //         tm.characterSize = 0.03f;
    //         tm.anchor = TextAnchor.MiddleRight;
    //         tm.alignment = TextAlignment.Right;
    //         tm.color = rowColors[row];
    //         tm.GetComponent<MeshRenderer>().material = font.material;
    //     }
    // }

    // void CreateLabels()
    // {
    //     // Labels show both frequency and FFT bin index at representative points.
    //     for (int row = 0; row < RowCount; row++)
    //     {
    //         for (int i = 0; i < labelCount; i++)
    //         {
    //             int displayIndex = labelDisplayIndices[i];
    //             int fftBin = mappedBins[row, displayIndex];
    //             float hz = mappedHz[row, displayIndex];

    //             GameObject go = new GameObject($"Label_{row}_{i}");
    //             go.transform.SetParent(transform);
    //             go.transform.position = GetBasePosition(row, displayIndex) + Vector3.up * labelYOffset;

    //             TextMesh tm = go.AddComponent<TextMesh>();
    //             tm.font = font;
    //             tm.text = $"{hz:0} Hz\nFFT[{fftBin}]";
    //             tm.fontSize = 70;
    //             tm.characterSize = 0.025f;
    //             tm.anchor = TextAnchor.LowerCenter;
    //             tm.alignment = TextAlignment.Center;
    //             tm.color = Color.white;
    //             tm.GetComponent<MeshRenderer>().material = font.material;

    //             labels[row, i] = tm;
    //         }
    //     }
    // }


    Vector3 GetBasePosition(int row, int displayIndex)
    {
        // All rows share the same x spacing; each row is shifted downward.
        return origin + new Vector3(displayIndex * xStep, -row * rowGap, 0f);
    }

    static int[] BuildRepresentativeIndices(int count, int size)
    {
        // Evenly distribute label positions across the displayed bins.
        int[] indices = new int[count];
        for (int i = 0; i < count; i++)
        {
            indices[i] = Mathf.RoundToInt(i * (size - 1) / (float)(count - 1));
        }
        return indices;
    }

    static float HzToMel(float hz)
    {
        // Standard mel conversion (Stevens, Volkmann, Newman approximation).
        return 2595f * Mathf.Log10(1f + hz / 700f);
    }

    static float MelToHz(float mel)
    {
        // Inverse of HzToMel.
        return 700f * (Mathf.Pow(10f, mel / 2595f) - 1f);
    }
}