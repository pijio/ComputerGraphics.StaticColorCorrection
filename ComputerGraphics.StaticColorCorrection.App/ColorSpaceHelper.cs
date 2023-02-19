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
        public static List<Matrix<double>> BitmapToRgbMatrix(Bitmap source, bool compress=true)
        {
            var rect = new Rectangle(0, 0, source.Width, source.Height);
            var picData = source.LockBits(rect, ImageLockMode.ReadOnly, source.PixelFormat);

            var size = Math.Abs(picData.Stride) * source.Height;
            var rgbs = new byte[size];

            Marshal.Copy(picData.Scan0, rgbs, 0, size);

            source.UnlockBits(picData);
            var result = new List<Matrix<double>>(source.Width * source.Height);
            var compressConst = 235d / 255d;
            for (int i = 0; i < rgbs.Length / 3; i++)
            {
                result.Add(Matrix<double>.Build.DenseOfColumnArrays(new[]
                {
                    (double)rgbs[i * 3 + 2] / 255d * (compress ? compressConst : 1d),
                    (double)rgbs[i * 3 + 1] / 255d * (compress ? compressConst : 1d),
                    (double)rgbs[i * 3] / 255d * (compress ? compressConst : 1d)
                }));
            }
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
            for (int i = 0; i < 3; i++)
            {
                var sum = 0d;
                foreach (var pixel in source.ImageColorSpaceContainer)
                {
                    sum += pixel[i, 0];
                }
                result.Add(sum / source.ImageColorSpaceContainer.Count);
            }
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
            for (int i = 0; i < 3; i++)
            {
                var sum = 0d;
                foreach (var pixel in source.ImageColorSpaceContainer)
                {
                    sum += Math.Pow(pixel[i, 0] - mathExp[i], 2);
                }
                result.Add(Math.Sqrt(sum/source.ImageColorSpaceContainer.Count));
            }
            return result;
        }

        /// <summary>
        /// Расчитываем новое значение цветового канала
        /// </summary>
        /// <param name="channelValue">значение цветового канала</param>
        /// <param name="mathexp">массив где 1 элемент - м.о. накладываемого канала, 2 элемент - м.о. целевого канала</param>
        /// <param name="contrast">контраст, можно задать отношением двух дисперсий канала источника и цели</param>
        /// <returns></returns>
        private static double CalculateUpdatedChannel(double channelValue, List<double> mathexp, double contrast)
        {
            return mathexp[0] + (channelValue - mathexp[1]) * contrast;
        }


        /// <summary>
        /// Наложение одной картинки на другой. Расчеты производятся в прострастве lab/hsl с последующим возвращением в rgb
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static List<Bitmap> MergePictures(Bitmap source, Bitmap target, bool[] includedSpaces, double? customContract = null)
        {
            var result = new List<Bitmap>(2);
            // переводим битмапы в rgb
            var sourceRgb = new Rgb(BitmapToRgbMatrix(source));
            var targetRgb = new Rgb(BitmapToRgbMatrix(target));
            #region rgb to lab and merge
            if (includedSpaces[0])
            {

                // переводим битмапы в lab, с промежуточным пространством Lms
                var sourceLab = InvokeConvertChain<Rgb, Lab>(sourceRgb, typeof(Lms));
                var targetLab = InvokeConvertChain<Rgb, Lab>(targetRgb, typeof(Lms));

                // считаем мат ожидание для источника и таргета
                var sourceMathExp = MathExpectation(sourceLab);
                var tagetMathExp = MathExpectation(targetLab);

                var mathExp = new List<List<double>> // массив матожиданий по 3 каналам для источника и таргета
            {
                new List<double>() { sourceMathExp[0], tagetMathExp[0] }, // 1 канал
                new List<double>() { sourceMathExp[1], tagetMathExp[1] }, // ...
                new List<double>() { sourceMathExp[2], tagetMathExp[2] }
            };

                // считаем дисперсию для источника и таргета
                var sourceVariance = Variance(sourceLab, sourceMathExp);
                var targetVariance = Variance(targetLab, tagetMathExp);

                var variance = new List<List<double>> // массив дисперсий по 3 каналам для источника и таргета
            {
                new List<double>() { sourceVariance[0], targetVariance[0] },// 1 канал
                new List<double>() { sourceVariance[1], targetVariance[1] },// ...
                new List<double>() { sourceVariance[2], targetVariance[2] }
            };

                // функция наложения источника на цель
                var funcForMutate = new Func<List<Matrix<double>>, int, List<double>>((targetSpace, channelNo) =>
                    targetSpace.AsParallel().AsOrdered().Select(
                    x =>
                        CalculateUpdatedChannel(x[channelNo, 0], mathExp[channelNo], customContract ?? variance[channelNo][0] / variance[channelNo][1])).ToList());

                //расчитываем каждый канал в отдельный список
                var firstTargetChannelMutated = funcForMutate(targetLab.ImageColorSpaceContainer, 0);
                var secondTargetChannelMutated = funcForMutate(targetLab.ImageColorSpaceContainer, 1);
                var thirdTargetChannelMutated = funcForMutate(targetLab.ImageColorSpaceContainer, 2);

                var newSpace = new List<Matrix<double>>();
                for (int i = 0; i < targetLab.ImageColorSpaceContainer.Count; i++)
                {
                    //заполняем новый список матрицами пикселей
                    newSpace.Add(Matrix<double>.Build.DenseOfColumnArrays(new[] { firstTargetChannelMutated[i], secondTargetChannelMutated[i], thirdTargetChannelMutated[i] }));
                }

                var compressConst = 235d / 255;
                var newLab = new Lab(newSpace);

                var mathexpnew = MathExpectation(newLab);
                var varianceNew = Variance(newLab, mathexpnew);

                //инициируем цепочку преобразований от лаб до ргб с учетом отрицательных элементов в матрице и сжатия ргб перед преобразованием в лаб
                var resultMap = InvokeConvertChain<Lab, Rgb>(newLab, typeof(Lms)).ImageColorSpaceContainer
                    .AsParallel().AsOrdered()
                    .Select(x => x.Map(y =>
                    {
                        if (y < 0) return 0;
                        if (y > 1) return compressConst;
                        return y;
                    })).ToList();
                result.Add(RgbMatrixToBitmap(resultMap, target.Width, target.Height));
                //result.Add(RgbMatrixToBitmap(InvokeConvertChain<Lab, Rgb>(targetLab, typeof(Lms)).ImageColorSpaceContainer, target.Width, target.Height));
            }
            else
            {
                result.Add(null);
            }
            #endregion

            #region rgb to hsl and merge

            if (includedSpaces[1])
            {
                sourceRgb = new Rgb(BitmapToRgbMatrix(source, false));
                targetRgb = new Rgb(BitmapToRgbMatrix(target, false));

                var sourceHsl = sourceRgb.ChangeColorSpace<Hsl>();
                var targetHsl = targetRgb.ChangeColorSpace<Hsl>();

                var sourceMathExpHsl = MathExpectation(sourceHsl);
                var targetMathExpHsl = MathExpectation(targetHsl);

                var mathExpHsl = new List<List<double>> // массив матожиданий по 3 каналам для источника и таргета
                {
                    new List<double>() { sourceMathExpHsl[0], targetMathExpHsl[0] }, // 1 канал
                    new List<double>() { sourceMathExpHsl[1], targetMathExpHsl[1] }, // ...
                    new List<double>() { sourceMathExpHsl[2], targetMathExpHsl[2] }
                };

                var sourceVarianceHsl = Variance(sourceHsl, sourceMathExpHsl);
                var targetVarianceHsl = Variance(targetHsl, targetMathExpHsl);

                var varianceHsl = new List<List<double>> // массив дисперсий по 3 каналам для источника и таргета
                {
                    new List<double>() { sourceVarianceHsl[0], targetVarianceHsl[0] },// 1 канал
                    new List<double>() { sourceVarianceHsl[1], targetVarianceHsl[1] },// ...
                    new List<double>() { sourceVarianceHsl[2], targetVarianceHsl[2] }
                };

                // функция наложения источника на цель
                var funcForMutateHsl = new Func<List<Matrix<double>>, int, List<double>>((targetSpace, channelNo) =>
                    targetSpace.AsParallel().AsOrdered().Select(
                        x =>
                            CalculateUpdatedChannel(x[channelNo, 0], mathExpHsl[channelNo], customContract ?? varianceHsl[channelNo][0] / varianceHsl[channelNo][1])).ToList());
                //расчитываем каждый канал в отдельный список
                var hslFirstMutated = funcForMutateHsl(targetHsl.ImageColorSpaceContainer, 0);
                var hslSecMutated = funcForMutateHsl(targetHsl.ImageColorSpaceContainer, 1);
                var hslThirdMutated = funcForMutateHsl(targetHsl.ImageColorSpaceContainer, 2);

                var newSpaceHsl = new List<Matrix<double>>();
                for (int i = 0; i < targetHsl.ImageColorSpaceContainer.Count; i++)
                {
                    //заполняем новый список матрицами пикселей
                    newSpaceHsl.Add(Matrix<double>.Build.DenseOfColumnArrays(new[] { hslFirstMutated[i], hslSecMutated[i], hslThirdMutated[i] }));
                }

                result.Add(RgbMatrixToBitmap(new Hsl(newSpaceHsl).ChangeColorSpace<Rgb>().ImageColorSpaceContainer.Select(x => x.Map(
                    y =>
                    {
                        if (y < 0) return 0;
                        if (y > 1) return 1;
                        return y;
                    })).ToList(), target.Width, target.Height));
            }
            else
            {
                result.Add(null);
            }

            #endregion

            return result;
        }
    } 
}
