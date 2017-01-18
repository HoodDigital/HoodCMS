﻿using SendGrid.Helpers.Mail;
using System.IO;

namespace Hood.Services
{
    public class MailObject
    {
        public Email To { get; set; }
        public string Subject { get; set; }
        public string PreHeader { get; set; }

        public MailObject()
        {
            _textBody = new StringWriter();
            _body = new StringWriter();
        }

        private StringWriter _body;
        public string ToHtmlString()
        {
            return _body.ToString();
        }

        private StringWriter _textBody;
        public override string ToString()
        {
            return _textBody.ToString();
        }

        public void AddH1(string content, string colour = "#222222", string align = "left")
        {
            _textBody.WriteLine();
            _textBody.WriteLine(content);
            _textBody.WriteLine();
            _textBody.WriteLine();
            _body.WriteLine(string.Format(@"<h1 class='align-{2}' style='color: {1}; font-family: sans-serif; font-weight: 300; line-height: 1.4; margin: 0; margin-bottom: 30px; font-size: 35px; text-transform: capitalize; text-align: {2};'>{0}</h1>", content, colour, align));
        }
        public void AddH2(string content, string colour = "#222222", string align = "left")
        {
            _textBody.WriteLine();
            _textBody.WriteLine(content);
            _textBody.WriteLine();
            _textBody.WriteLine();
            _body.WriteLine(string.Format(@"<h2 class='align-{2}' style='color: {1}; font-family: sans-serif; font-weight: 300; line-height: 1.4; margin: 0; margin-bottom: 30px; font-size: 30px; text-transform: capitalize; text-align: {2};'>{0}</h2>", content, colour, align));
        }
        public void AddH3(string content, string colour = "#222222", string align = "left")
        {
            _textBody.WriteLine();
            _textBody.WriteLine(content);
            _textBody.WriteLine();
            _textBody.WriteLine();
            _body.WriteLine(string.Format(@"<h3 class='align-{2}' style='color: {1}; font-family: sans-serif; font-weight: 300; line-height: 1.4; margin: 0; margin-bottom: 30px; font-size: 25px; text-transform: capitalize; text-align: {2};'>{0}</h3>", content, colour, align));
        }
        public void AddH4(string content, string colour = "#222222", string align = "left")
        {
            _textBody.WriteLine();
            _textBody.WriteLine(content);
            _textBody.WriteLine();
            _textBody.WriteLine();
            _body.WriteLine(string.Format(@"<h4 class='align-{2}' style='color: {1}; font-family: sans-serif; font-weight: 300; line-height: 1.4; margin: 0; margin-bottom: 30px; font-size: 20px; text-transform: capitalize; text-align: {2};'>{0}</h4>", content, colour, align));
        }
        public void AddParagraph(string content, string colour = "#222222", string align = "left")
        {
            _textBody.WriteLine(content);
            _textBody.WriteLine();
            _body.WriteLine(string.Format(@"<p class='align-{2}' style='color: {1}; font-family: sans-serif; font-size: 14px; font-weight: normal; margin: 0; margin-bottom: 15px; text-align: {2};'>{0}</p>", content, colour, align));
        }
        public void AddImage(string url, string altText)
        {
            _textBody.WriteLine("Image: " + url + "(" + altText + ")");
            _body.WriteLine(string.Format(@"<p style='font-family: sans-serif; font-size: 14px; font-weight: normal; margin: 0; margin-bottom: 15px;'><img src='{0}' alt='{1}' width='520' class='img-responsive img-block' style='border: none; -ms-interpolation-mode: bicubic; max-width: 100%; display: block;'></p>", url, altText));
        }        
        public void AddCallToAction(string content, string url, string colour = "#3498db", string align = "left")
        {
            _textBody.WriteLine(content + ": " + url);
            _body.WriteLine(string.Format(@"<table border='0' cellpadding='0' cellspacing='0' class='btn btn-primary' style='border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: 100%; box-sizing: border-box; min-width: 100% !important;' width='100%'>
    <tbody>
        <tr>
            <td align='{3}' style='font-family: sans-serif; font-size: 14px; vertical-align: top; padding-bottom: 15px;' valign='top'>
                <table border='0' cellpadding='0' cellspacing='0' style='border-collapse: separate; mso-table-lspace: 0pt; mso-table-rspace: 0pt; width: auto;'>
                    <tbody>
                        <tr>
                            <td style='font-family: sans-serif; font-size: 14px; vertical-align: top; background-color: {2}; border-radius: 5px; text-align: {3};' valign='top' bgcolor='{2}' align='{3}'>
                                <a href='{1}' target='_blank' style='display: inline-block; color: #ffffff; background-color: {2}; border: solid 1px {2}; border-radius: 5px; box-sizing: border-box; cursor: pointer; text-decoration: none; font-size: 14px; font-weight: bold; margin: 0; padding: 12px 25px; text-transform: capitalize; border-color: {2};'>
                                    {0}
                                </a>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </td>
        </tr>
    </tbody>
</table>
", content, url, colour, align));
        }

    }
}