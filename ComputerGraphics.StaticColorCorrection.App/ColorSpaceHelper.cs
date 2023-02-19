using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using ComputerGraphics.StaticColorCorrection.App.ColorSpaces;

namespace ComputerGraphics.StaticColorCorrection.App
{
    public partial class ColorSpaceHelper
    {
        /// <summary>
        /// Преобразование Bitmap в матрицу вещественных rgb
        /// </summary>
        /// <returns></returns>
        public static List<Matrix<double>> BitmapToRgbMatrix(Bitmap source)
        {
            //var watcher = new Stopwatch();
            //watcher.Start();
            //var result = new List<Matrix<double>>(source.Width * source.Height);
            //var compressConst = 235d / 255;
            //for (int i = 0; i < source.Height; i++)
            //{
            //    for (int j = 0; j < source.Width; j++)
            //    {
            //        var color = source.GetPixel(j,i);
            //        result.Add(Matrix<double>.Build.DenseOfColumnArrays(new[]
            //        {

            //            (double)color.R / 255 * compressConst,
            //            (double)color.G / 255 * compressConst,
            //            (double)color.B / 255 * compressConst
            //        }));

            //    }
            //}
            var rect = new Rectangle(0, 0, source.Width, source.Height);
            var picData = source.LockBits(rect, ImageLockMode.ReadOnly, source.PixelFormat);

            var size = Math.Abs(picData.Stride) * source.Height;
            var rgbs = new byte[size];

            Marshal.Copy(picData.Scan0, rgbs, 0, size);

            source.UnlockBits(picData);
            var result = new List<Matrix<double>>(size);
            var compressConst = 235d / 255;
            for (int i = 0; i < rgbs.Length / 3; i++)
            {
                result.Add(Matrix<double>.Build.DenseOfColumnArrays(new[]
                {
                    (double)rgbs[i * 3 + 2] / 255 * compressConst,
                    (double)rgbs[i * 3 + 1] / 255 * compressConst,
                    (double)rgbs[i * 3] / 255 * compressConst
                }));
            }
            //watcher.Stop();
            //var time = watcher.ElapsedMilliseconds;
            return result;
        }

        public static Bitmap RgbMatrixToBitmap(List<Matrix<double>> rgbSpace, int width, int height)
        {
            byte[] rgbs = new byte[rgbSpace.Count * 3];

            int byteIndex = 0;
            foreach (var color in rgbSpace)
            {
                rgbs[byteIndex++] = (byte)(color[2,0] * 255.0); // red
                rgbs[byteIndex++] = (byte)(color[1,0] * 255.0); // green
                rgbs[byteIndex++] = (byte)(color[0,0] * 255.0); // blue
            }

            var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bmp.PixelFormat);

            try
            {
                Marshal.Copy(rgbs, 0, bmpData.Scan0, rgbs.Length);
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }

            return bmp;
        }

        /// <summary>
        /// Цепочка преобразований одного цветого пространства в другое
        /// </summary>
        /// <typeparam name="TIn">Цветовое пространство на входе</typeparam>
        /// <typeparam name="TOut">Цветовое пространство на выходе</typeparam>
        /// <param name="source">Исходный битмап в TIn пространстве</param>
        /// <param name="chainTypes">Цепочка пространств по которой битмап пройдет попытаясь достигнуть TOut пространства.
        /// Не обязательно если происходит преобразование в одно действие.
        /// Указание изначального и конечного пространства не нужно</param>
        /// <returns>Целевой битмап в заданном пространстве</returns>
        public static TOut InvokeConvertChain<TIn, TOut>(TIn source, params Type[] chainTypes)
            where TIn : BaseColorSpace
            where TOut : BaseColorSpace
        {

            if (chainTypes.Length == 0)
            {
                return source.ChangeColorSpace<TOut>();
            }

            //var watcher = new Stopwatch();
            //watcher.Start();
            object currentSource = source;
            foreach (var type in chainTypes)
            {
                currentSource = currentSource.GetType().GetMethod("ChangeColorSpace")
                    .MakeGenericMethod(type)
                    .Invoke(currentSource, null);
            }

            var result = (TOut)currentSource.GetType().GetMethod("ChangeColorSpace")
                .MakeGenericMethod(typeof(TOut))
                .Invoke(currentSource, null);
            //watcher.Stop();
            //var time = watcher.ElapsedMilliseconds;
            return result;
        }

        /// <summary>
        /// Расчет мат ожидания по 3 каналам у битмапа в цветовом пространстве
        /// </summary>
        /// <param name="source"></param>
        /// <returns>список из 3 элементов в котором последовательно для каждого канала идет его мат ожидание</returns>
        public static List<double> MathExpectation(BaseColorSpace source)
        {
            var result = new List<double>(3);
            var formula = new Func<int, double>(channelNo =>
                source.ImageColorSpaceContainer.AsParallel().Sum(x => x[channelNo, 0]) /
                source.ImageColorSpaceContainer.Count);
            result.Add(formula(0));
            result.Add(formula(1));
            result.Add(formula(2));
            return result;
        }

        /// <summary>
        /// Расчет дисперсию по 3 каналам у битмапа в цветовом пространстве
        /// </summary>
        /// <param name="source"></param>
        /// <returns>список из 3 элементов в котором последовательно для каждого канала идет его дисперсия</returns>
        private static List<double> Variance(BaseColorSpace source, List<double> mathExp = null)
        {
            if (mathExp == null)
                mathExp = MathExpectation(source);

            var result = new List<double>(3);
            var formula = new Func<double, double, double>((double channel, double matexp) => Math.Pow(channel - matexp, 2));
            var sumByChannels = new Func<int, double>(channelNo =>
                Math.Sqrt(source.ImageColorSpaceContainer.Sum(x => formula(x[channelNo, 0], mathExp[channelNo])) /
                          source.ImageColorSpaceContainer.Count));
            result.Add(sumByChannels(0));
            result.Add(sumByChannels(1));
            result.Add(sumByChannels(2));
            return result;
        }

        /// <summary>
        /// Расчитываем новое значение цветового канала
        /// </summary>
        /// <param name="channelValue">значение цветового канала</param>
        /// <param name="mathexp">массив где 1 элемент - м.о. накладываемого канала, 2 элемент - м.о. целевого канала</param>
        /// <param name="variance">массив где 1 элемент - дисперсия. накладываемого канала, 2 элемент - дисперсия целевого канала</param>
        /// <returns></returns>
        private static double CalculateUpdatedChannel(double channelValue, List<double> mathexp, List<double> variance)
        {
            return mathexp[0] + (channelValue - mathexp[1]) * (variance[0] / variance[1]);
        }

        public static Bitmap MergePictures(Bitmap source, Bitmap target)
        {
            var sourceRgb = new Rgb(BitmapToRgbMatrix(source));
            var targetRgb = new Rgb(BitmapToRgbMatrix(target));

            var sourceLab = InvokeConvertChain<Rgb, Lab>(sourceRgb, typeof(Lms));
            var targetLab = InvokeConvertChain<Rgb, Lab>(targetRgb, typeof(Lms));

            var sourceMathExp = MathExpectation(sourceLab);
            var tagetMathExp = MathExpectation(targetLab);

            var mathExp = new List<List<double>> // массив матожиданий по 3 каналам для источника и таргета
            {
                new List<double>() { sourceMathExp[0], tagetMathExp[0] },
                new List<double>() { sourceMathExp[1], tagetMathExp[1] },
                new List<double>() { sourceMathExp[2], tagetMathExp[2] }
            };

            var sourceVariance = Variance(sourceLab, sourceMathExp);
            var targetVariance = Variance(targetLab, tagetMathExp);

            var variance = new List<List<double>> // массив дисперсий по 3 каналам для источника и таргета
            {
                new List<double>() { sourceVariance[0], targetVariance[0] },
                new List<double>() { sourceVariance[1], targetVariance[1] },
                new List<double>() { sourceVariance[2], targetVariance[2] }
            };

            var funcForMutate = new Func<int, List<double>>(channelNo =>
                targetLab.ImageColorSpaceContainer.AsParallel().AsOrdered().Select(
                x =>
                    CalculateUpdatedChannel(x[channelNo, 0], mathExp[channelNo], variance[channelNo])).ToList());
            var firstTargetChannelMutated = funcForMutate(0);
            var secondTargetChannelMutated = funcForMutate(1);
            var thirdTargetChannelMutated = funcForMutate(2);

            var newSpace = new List<Matrix<double>>();
            for (int i = 0; i < targetLab.ImageColorSpaceContainer.Count; i++)
            {
                newSpace.Add(Matrix<double>.Build.DenseOfColumnArrays(new[] {firstTargetChannelMutated[i], secondTargetChannelMutated[i], thirdTargetChannelMutated[i]}));
            }

            var compressConst = 235d / 255;
            var resultMap = InvokeConvertChain<Lab, Rgb>(new Lab(newSpace), typeof(Lms)).ImageColorSpaceContainer
                .AsParallel().AsOrdered()
                .Select(x => x.Map(y =>
                {
                    if (y < 0) return 0;
                    if (y > compressConst) return 1;
                    return y;
                })).ToList();
            var test = InvokeConvertChain<Lab, Rgb>(targetLab, typeof(Lms)).ImageColorSpaceContainer.Select(x => x.Map(y =>
                {
                    if (y < 0) return 0;
                    if (y > compressConst) return 1;
                    return y;
                })).ToList();
            return RgbMatrixToBitmap(resultMap, target.Width, target.Height);
        }
    } 
}
