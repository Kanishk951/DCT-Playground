using UnityEngine;
using Color = UnityEngine.Color;
using System.Collections.Generic;

public static class ImageManager
{
    
    
    public static MyYCbCr[,] ChromaSumbsample(MyYCbCr[,] input, int batch_side)
    {
        if (input.Length < 1)
        {
            return null;
        }

        if (input.Length % batch_side != 0)
        {
            Debug.LogError(
                $"input length (in this case is {input.Length}) must be a multiple of the batch size that in this case is {batch_side}");
                return null;
        }

        MyYCbCr[,] subsampled_image = input;

        for (int y = 0; y < input.GetLength(0); y += batch_side)
        {
            for (int x = 0; x < input.GetLength(1); x += batch_side)
            {
                MyYCbCr myYCbCr = input[y, x];
                for (int i = x + 1; i < x + batch_side; i++)
                {
                    if (i > input.GetLength(1) - 1)
                    {
                        break;
                    }

                    subsampled_image[y, i].cr = myYCbCr.cr;
                    subsampled_image[y, i].cb = myYCbCr.cb;
                }

                for (int j = y + 1; j < y + batch_side; j++)
                {
                    if (j > input.GetLength(0) - 1) break;

                    subsampled_image[j, x].cr = myYCbCr.cr;
                    subsampled_image[j, x].cb = myYCbCr.cb;
                }
            }
        }
        
        return subsampled_image;
    }

    public static Texture2D SauceToTexture(string sauce)
    {
        if (!System.IO.File.Exists(sauce))
        {
            Debug.LogError("Image not found.");
            return null;
        }

        var raw_data = System.IO.File.ReadAllBytes(sauce);
        Texture2D image = new Texture2D(2, 2); // side note width and height dont matter here.
        image.LoadImage(raw_data);
        if (image.height % 8 != 0 || image.width % 8 != 0)
        {
            Debug.LogError("The weigth and width of the image must be multiples of 8.");
        }
        return image;
    }

    public static MyYCbCr[,] ConvertToYCbCr(Texture2D image)
    {
        if (image == null)
        {
            Debug.LogError("Image input is null.");
            return null;
        }

        MyYCbCr[,] image_in_ycbcr = new MyYCbCr[image.height, image.width];
        
        Debug.Log($"image width and height is {image.width}, {image.height}");
        
        for (int y = 0; y < image.height; y++)
        {
            for (int x = 0; x < image.width; x++)
            {
                
                Color color = image.GetPixel(x, y);
                MyYCbCr for_this_pixel = new MyYCbCr();

                float r = color.r * 255;
                float g = color.g * 255;
                float b = color.b * 255;

                for_this_pixel.lum = (0.299f*r) + (0.587f*g)  + (0.114f*b);
                for_this_pixel.cb = 128f - (.168736f * r) - (.331364f * g) + (.5f * b);
                for_this_pixel.cr = 128f + (.5f * r) - (.418688f * g) - (.081312f * b); 
                image_in_ycbcr[y, x] = for_this_pixel;
            }
        }

        return image_in_ycbcr;
    }

    public static Texture2D ConvertToRGBImage(MyYCbCr[,] input)
    { 

        Debug.Log($"input has size of {input.GetLength(1)} * {input.GetLength(0)}");
        
        Texture2D rgb_image = new Texture2D(input.GetLength(1), input.GetLength(0));
        for (int y = 0; y < input.GetLength(0); y++)
        {
            for (int x = 0; x < input.GetLength(1); x++)
            {
                float Y = (float) input[y, x].lum;
                float Cb = (float) input[y, x].cb;
                float Cr = (float) input[y, x].cr;
                
                Cr -= 128;
                Cb -= 128;
                float r = (Y + (1.402f * Cr));
                float g = (Y - (0.34414f * Cb) - (.71414f * Cr));
                float b = (Y + (1.772f * Cb));
        
                r = Mathf.Max(0, Mathf.Min(255.0f, r));
                g = Mathf.Max(0, Mathf.Min(255.0f, g));
                b = Mathf.Max(0, Mathf.Min(255.0f, b));
        
                Color color = new Color(r/255.0f, g/255.0f, b/255.0f, 1);
                rgb_image.SetPixel(x, y, color);
            }
        }
        rgb_image.Apply();
        return rgb_image;
    }

    static void center_the_values(ref MyYCbCr[,] input)
    {
        for (int y = 0; y < input.GetLength(0); y++)
        {
            for (int x = 0; x < input.GetLength(1); x++)
            {
                input[y, x].lum -= 128;
                input[y, x].cb -= 128;
                input[y, x].cr -= 128;
            }
        }
    }

    static void decenter_the_values(ref MyYCbCr[,] input) {
        for (int y = 0; y < input.GetLength(0); y++) {
            for (int x = 0; x < input.GetLength(1); x++) {
                input[y, x].lum += 128;
                input[y, x].cb += 128;
                input[y, x].cr += 128;
            }
        }
    }

    public static MyYCbCr[,] ApplyDCT(MyYCbCr[,] input_space)
    {
        center_the_values(ref input_space);
        MyYCbCr[,] output_space = input_space;
         
        for (int y = 0; y < input_space.GetLength(0); y += 8)
        {
            for (int x = 0; x < input_space.GetLength(1); x += 8)
            {
                for (int j = y; j < y + 8; j++) {
                    for (int i = x; i < x + 8; i++) {
                        int i_relative = i - x;
                        int j_relative = j - y;

                        float c_i = C(u: i_relative);
                        float c_j = C(u: j_relative);
                        MyYCbCr sum_fun_stuff = SumyStuff(x, y, i_relative, j_relative, input_space);
                        float new_lum = 0.25f * c_i * c_j * sum_fun_stuff.lum;
                        float new_cb = 0.25f * c_i * c_j * sum_fun_stuff.cb;
                        float new_cr = 0.25f * c_i * c_j *sum_fun_stuff.cr;
                        output_space[j, i].lum = new_lum;
                        output_space[j, i].cb = new_cb;
                        output_space[j, i].cr = new_cr;
                    }
                }
            }
        }

        return output_space;
    }

    public static MyYCbCr[,] InverseDCT(MyYCbCr[,] input_space) {
        MyYCbCr[,] output_space = input_space;

        // Again There probably is a better way to do this but my smooth brain right now can only think of nothing but this.
        for (int y = 0; y < input_space.GetLength(0); y += 8)
        {
            for (int x = 0; x < input_space.GetLength(1); x += 8)
            {
                for (int j = y; j < y + 8; j++) {
                    for (int i = x; i < x + 8; i++) {
                        MyYCbCr sumy_output = SumyStuffForIDCT(x, y, (i - x), (j - y), input_space);
                        output_space[j, i].lum = .25f * sumy_output.lum;
                        output_space[j, i].cb = .25f * sumy_output.cb;
                        output_space[j, i].cr = .25f * sumy_output.cr;
                    }
                }
            }
        }
        decenter_the_values(ref output_space);
        return output_space;
    }

    static MyYCbCr SumyStuff(int start_x, int start_y, int i, int j, MyYCbCr[,] input)
    {
        MyYCbCr x_sum;
        x_sum.lum = 0;
        x_sum.cb = 0;
        x_sum.cr = 0;

        for (int x = start_x; x < start_x + 8; x++)
        {
            MyYCbCr y_sum;
            y_sum.lum = 0;
            y_sum.cb = 0;
            y_sum.cr = 0;
            for (int y = start_y; y < start_y + 8; y++)
            {
                float pi = Mathf.PI;
                float cos_i = Mathf.Cos(((2 * (x - start_x) + 1) * i * pi)*0.0625f);
                float cos_j = Mathf.Cos(((2 * (y - start_y) + 1) * j * pi)*0.0625f);

                float coefficient = cos_i * cos_j;

                y_sum.lum += (float)input[y, x].lum * coefficient;
                y_sum.cb += (float)input[y, x].cb * coefficient;               
                y_sum.cr += (float)input[y, x].cr * coefficient;               
            }

            x_sum.lum += y_sum.lum;
            x_sum.cb += y_sum.cb;
            x_sum.cr += y_sum.cr;
        }

        return x_sum;
    }

    static MyYCbCr SumyStuffForIDCT(int start_x, int start_y, int i, int j, MyYCbCr[,] input) {
        MyYCbCr x_sum;
        x_sum.lum = 0;
        x_sum.cb = 0;
        x_sum.cr = 0;


        for (int x = start_x; x < start_x + 8; x++) {
            MyYCbCr y_sum;
            y_sum.lum = 0;
            y_sum.cb = 0;
            y_sum.cr = 0;
            for (int y = start_y; y <  start_y + 8; y++) {
                int x_in_reference = x - start_x;
                int y_in_reference = y - start_y;

                float c_x = C(x_in_reference);
                float c_y = C(y_in_reference);

                float pi = Mathf.PI;
                float cos_x = Mathf.Cos(((2 * i + 1) * x_in_reference * pi) * 0.0625f);
                float cos_y = Mathf.Cos(((2 * j + 1) * y_in_reference * pi) * 0.0625f);

                float coefficient = cos_x * cos_y;

                y_sum.lum += (float) (c_x * c_y * input[y, x].lum * coefficient); 
                y_sum.cb += (float) (c_x * c_y * input[y, x].cb * coefficient); 
                y_sum.cr += (float) (c_x * c_y * input[y, x].cr * coefficient); 
            }

            x_sum.lum += y_sum.lum;
            x_sum.cb += y_sum.cb;
            x_sum.cr += y_sum.cr;
        }

        return x_sum;
    }

    public static MyYCbCr[,] Quantize(Dictionary<int, int[]> quantization_table, MyYCbCr[,] input) {
        if (quantization_table == null || quantization_table.Count != 3 || quantization_table[0].Length != 8*8) {
            Debug.LogError("check your quantization table mate.");
            return null;
        }

        MyYCbCr[,] output = input;

        for (int y = 0; y < input.GetLength(0); y += 8)
        {
            for (int x = 0; x < input.GetLength(1); x += 8)
            {
                for (int j = y; j < y + 8; j++) {

                    for (int i = x; i < x + 8; i++) {

                        int index_in_quant_table = ((j - y) * 8) + (i - x);

                        float new_lum = input[j, i].lum / quantization_table[0][index_in_quant_table];
                        float new_cb = input[j, i].cb / quantization_table[1][index_in_quant_table];
                        float new_cr = input[j, i].cr / quantization_table[2][index_in_quant_table];

                        output[j, i].lum = Mathf.RoundToInt(new_lum);
                        output[j, i].cb = Mathf.RoundToInt(new_cb);
                        output[j, i].cr =  Mathf.RoundToInt(new_cr);
                    }
                }
            }
        }

        return output;
    }

    public static MyYCbCr[,] DeQuantize(Dictionary<int, int[]> quantization_table, MyYCbCr[,] input) {
        MyYCbCr[,] output = input;

        if (quantization_table == null || quantization_table.Count != 3 || quantization_table[0].Length != 8*8) {
            Debug.LogError("check your quantization table mate.");
            return null;
        }

        for (int y = 0; y < input.GetLength(0); y += 8)
        {
            for (int x = 0; x < input.GetLength(1); x += 8)
            {
                for (int j = y; j < y + 8; j++) {
                    for (int i = x; i < x + 8; i++) {
                        int index_in_quant_table = ((j-y) * 8) + (i-x);

                        output[j, i].lum = input[j, i].lum * quantization_table[0][index_in_quant_table];
                        output[j, i].cb = input[j, i].cb * quantization_table[1][index_in_quant_table];
                        output[j, i].cr = input[j, i].cr * quantization_table[2][index_in_quant_table];
                    }
                }
            }
        }

        return output;
    }

    
    static float C(int u)
    {
        if (u < 0) {
            Debug.LogError("value of u is below 0");
            return -1;
        }

        if (u == 0)
        {
            return 1.0f / Mathf.Sqrt(2);
        }

        return 1.0f;
    }
}
