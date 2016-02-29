﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Discord.Commands;
using Discord;
using NadekoBot.Modules;
using System.IO;
using System.Drawing;
using NadekoBot.Classes;

namespace NadekoBot.Extensions {
    public static class Extensions
    {
        public static string Scramble(this string word) {

            var letters = word.ToArray();
            int count = 0;
            for (int i = 0; i < letters.Length; i++) {
                if (letters[i] == ' ')
                    continue;

                count++;
                if (count <= letters.Length / 5)
                    continue;

                if (count % 3 == 0)
                    continue;

                if (letters[i] != ' ')
                    letters[i] = '_';
            }
            return "`"+string.Join(" ", letters)+"`";
        }
        public static string TrimTo(this string str, int num) {
            if (num < 0)
                throw new ArgumentException("TrimTo argument cannot be less than 0");
            if (num == 0)
                return String.Empty;
            if (num <= 3)
                return String.Join("", str.Select(c => '.'));
            if (str.Length < num)
                return str;
            return string.Join("", str.Take(num - 3)) + "...";
        }
        /// <summary>
        /// Removes trailing S or ES (if specified) on the given string if the num is 1
        /// </summary>
        /// <param name="str"></param>
        /// <param name="num"></param>
        /// <param name="es"></param>
        /// <returns>String with the correct singular/plural form</returns>
        public static string SnPl(this string str, int? num,bool es = false) {
            if (str == null)
                throw new ArgumentNullException(nameof(str));
            if (num == null)
                throw new ArgumentNullException(nameof(num));
            return num == 1 ? str.Remove(str.Length - 1, es ? 2 : 1) : str;
        }

        /// <summary>
        /// Sends a message to the channel from which this command is called.
        /// </summary>
        /// <param name="e">EventArg</param>
        /// <param name="message">Message to be sent</param>
        /// <returns></returns>
        public static async Task<Message> Send(this CommandEventArgs e, string message) 
            => await e.Channel.SendMessage(message);

        /// <summary>
        /// Sends a message to the channel from which MessageEventArg came.
        /// </summary>
        /// <param name="e">EventArg</param>
        /// <param name="message">Message to be sent</param>
        /// <returns></returns>
        public static async Task Send(this MessageEventArgs e, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            await e.Channel.SendMessage(message);
        }

        /// <summary>
        /// Sends a message to this channel.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task Send(this Channel c, string message)
        {
            await c.SendMessage(message);
        }

        /// <summary>
        /// Sends a private message to this user.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task Send(this User u, string message)
        {
            await u.SendMessage(message);
        }

        /// <summary>
        /// Replies to a user who invoked this command, message start with that user's mention.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task Reply(this CommandEventArgs e, string message)
        {
            await e.Channel.SendMessage(e.User.Mention + " " + message);
        }

        /// <summary>
        /// Replies to a user who invoked this command, message start with that user's mention.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task Reply(this MessageEventArgs e, string message)
        {
            await e.Channel.SendMessage(e.User.Mention + " " + message);
        }

        /// <summary>
        /// Randomizes element order in a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void Shuffle<T>(this IList<T> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Shortens a string URL
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="action"></param>
        public static async Task<string> ShortenUrl(this string str) => await SearchHelper.ShortenUrl(str);

        /// <summary>
        /// Gets the program runtime
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="action"></param>
        public static string GetRuntime(this DiscordClient c) => ".Net Framework 4.5.2";

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action) {
            foreach (T element in source) {
                action(element);
            }
        }

        //http://www.dotnetperls.com/levenshtein
        public static int LevenshteinDistance(this string s, string t) {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0) {
                return m;
            }

            if (m == 0) {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++) {
            }

            for (int j = 0; j <= m; d[0, j] = j++) {
            }

            // Step 3
            for (int i = 1; i <= n; i++) {
                //Step 4
                for (int j = 1; j <= m; j++) {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        public static int KiB(this int value) => value * 1024;
        public static int KB(this int value) => value * 1000;

        public static int MiB(this int value) => value.KiB() * 1024;
        public static int MB(this int value) => value.KB() * 1000;

        public static int GiB(this int value) => value.MiB() * 1024;
        public static int GB(this int value) => value.MB() * 1000;

        public static Stream ToStream(this Image img, System.Drawing.Imaging.ImageFormat format = null) {
            if (format == null)
                format = System.Drawing.Imaging.ImageFormat.Jpeg;
            MemoryStream stream = new MemoryStream();
            img.Save(stream, format);
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Merges Images into 1 Image and returns a bitmap.
        /// </summary>
        /// <param name="images">The Images you want to merge.</param>
        /// <returns>Merged bitmap</returns>
        public static Bitmap Merge(this IEnumerable<Image> images,int reverseScaleFactor = 1) {
            if (images.Count() == 0) return null;
            int width = images.Sum(i => i.Width);
            int height = images.First().Height ;
            Bitmap bitmap = new Bitmap(width / reverseScaleFactor, height / reverseScaleFactor);
            var r = new Random();
            int offsetx = 0;
            foreach (var img in images) {
                Bitmap bm = new Bitmap(img);
                for (int w = 0; w < img.Width; w++) {
                    for (int h = 0; h < bitmap.Height; h++) {
                        bitmap.SetPixel(w / reverseScaleFactor + offsetx, h , bm.GetPixel(w, h *reverseScaleFactor));
                    }
                }
                offsetx += img.Width/reverseScaleFactor;
            }
            return bitmap;
        }
        /// <summary>
        /// Merges Images into 1 Image and returns a bitmap asynchronously.
        /// </summary>
        /// <param name="images">The Images you want to merge.</param>
        /// <returns>Merged bitmap</returns>
        public static async Task<Bitmap> MergeAsync(this IEnumerable<Image> images, int reverseScaleFactor = 1) =>
            await Task.Run(() => images.Merge(reverseScaleFactor));
    }
}
