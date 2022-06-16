using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Manager : MonoBehaviour
{
    public string image_sauce = @"";
    public int batch_side = 2;
    public int quality_factor = 50;
    public Image output;
    public Image input;

    Dictionary<int, int[]> quant_tables = new Dictionary<int, int[]>();

    // Start is called before the first frame update
    void Start()
    {
        // 0 stands for lum (Y), 1 stands for CB, 2 stands for CR.
        for (int i = 0; i < 3; i++) {
            using (StreamReader sr = new StreamReader($"QuantizationTables/{i}/{quality_factor}.txt")) {
                if (sr == null) {
                    Debug.LogError($"Quant. table with a quality factor of {quality_factor} for {i} could not be found.");
                    return;
                }
                string table_string = sr.ReadToEnd();
                quant_tables[i] = Array.ConvertAll(table_string.Split(','), int.Parse);
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChooseImage()
    {
        image_sauce =
            EditorUtility.OpenFilePanelWithFilters("Select an image to input", image_sauce,
                new[] { "Image files", "png,jpg,jpeg" });
        DoImageStuff(image_sauce);
    }
    
    /// <summary>
    /// Does image stuff...
    /// </summary>
    /// <param name="sauce">sauce</param>
    void DoImageStuff(string sauce)
    {
        Texture2D input_as_texture = ImageManager.SauceToTexture(sauce);

        if (input_as_texture != null)
        {
            DisplayImage(input_as_texture, input); // Renders input image to the input image box.
        }

        MyYCbCr[,] encoded_image = Encode(input_as_texture);

        if (encoded_image == null)
        {
            return;
        }

        MyYCbCr[,] dequantized_image = ImageManager.DeQuantize(quant_tables, encoded_image);
        MyYCbCr[,] compressed_image = ImageManager.InverseDCT(dequantized_image);
        Texture2D image_as_texture = ImageManager.ConvertToRGBImage(compressed_image);
        
        DisplayImage(image_as_texture, output); // displays the image only if the image object reference is not null
    }

    MyYCbCr[,] Encode(Texture2D input_as_texture) {
        MyYCbCr[,] image_in_ycbcr = ImageManager.ConvertToYCbCr(input_as_texture);
        MyYCbCr[,] sampled_image = ImageManager.ChromaSumbsample(image_in_ycbcr, batch_side);
        MyYCbCr[,] dct_coeff_image = ImageManager.ApplyDCT(sampled_image);

        MyYCbCr[,] quantisized_image = ImageManager.Quantize(quant_tables, dct_coeff_image);

        if (quantisized_image == null)
        {
            return null;
        }

        return quantisized_image;
    }

    /// <summary>
    /// Renders image passed as a `Texture2D` class object to the given `image_object'.
    /// </summary>
    /// <param name="image_as_texture"></param>
    /// <param name="image_object"></param>
    void DisplayImage(Texture2D image_as_texture, Image image_object)
    {
        if (output == null)
        {
            Debug.Log("cannot render image on the image object since it is null");
            return;
        }
        
        Sprite image_sprite = Sprite.Create(image_as_texture, new Rect(0.0f, 0.0f, image_as_texture.width, image_as_texture.height), new Vector2(0.5f, 0.5f), 100.0f);

        image_object.sprite = image_sprite;
    }
}
