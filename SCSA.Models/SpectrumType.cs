using System.ComponentModel.DataAnnotations;

namespace SCSA.Models
{
    /// <summary>
    /// Defines the types of spectrum analysis available.
    /// </summary>
    public enum SpectrumType
    {
        /// <summary>
        /// The magnitude spectrum.
        /// </summary>
        [Display(Name = "幅值谱")]
        Amplitude,

        /// <summary>
        /// The power spectrum.
        /// </summary>
        [Display(Name = "功率谱")]
        Power,

        /// <summary>
        /// The power spectral density.
        /// </summary>
        [Display(Name = "功率谱密度")]
        PowerSpectralDensity
    }
} 