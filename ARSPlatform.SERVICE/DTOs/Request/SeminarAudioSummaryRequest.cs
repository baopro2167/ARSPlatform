using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ARSPlatform.SERVICE.DTOs.Request
{
    public class SeminarAudioSummaryRequest
    {
        [Required(ErrorMessage = "File âm thanh không được để trống.")]
        public IFormFile AudioFile { get; set; } = null!;

        public bool ReplaceExisting { get; set; } = false;
    }
}
