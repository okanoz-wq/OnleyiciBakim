using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OnleyiciBakim.Models.Ai;

public sealed class MachineFeatureDto
{
    [JsonIgnore]
    public const int ModelFeatureCount = 20;

    [JsonIgnore]
    public string MachineId { get; set; } = string.Empty;

    [Range(0, double.MaxValue), JsonPropertyName("yas")]
    public double? Yas { get; set; }

    [Range(0, double.MaxValue), JsonPropertyName("son_bakimdan_gecen_gun")]
    public double? SonBakimdanGecenGun { get; set; }

    [Range(0, double.MaxValue), JsonPropertyName("bakim_sayisi_30g")]
    public double? BakimSayisi30G { get; set; }

    [Range(0, double.MaxValue), JsonPropertyName("bakim_sayisi_90g")]
    public double? BakimSayisi90G { get; set; }

    [Range(0, double.MaxValue), JsonPropertyName("ariza_sayisi_30g")]
    public double? ArizaSayisi30G { get; set; }

    [Range(0, double.MaxValue), JsonPropertyName("ariza_sayisi_90g")]
    public double? ArizaSayisi90G { get; set; }

    [Range(0, double.MaxValue), JsonPropertyName("toplam_gecmis_ariza")]
    public double? ToplamGecmisAriza { get; set; }

    [Range(0, double.MaxValue), JsonPropertyName("hata_sayisi_7g")]
    public double? HataSayisi7G { get; set; }

    [Range(0, double.MaxValue), JsonPropertyName("hata_sayisi_30g")]
    public double? HataSayisi30G { get; set; }

    [Range(0, double.MaxValue), JsonPropertyName("durus_suresi_30g")]
    public double? DurusSuresi30G { get; set; }

    [Range(0, double.MaxValue), JsonPropertyName("ort_mudahale_suresi_90g")]
    public double? OrtMudahaleSuresi90G { get; set; }

    [Range(0, double.MaxValue), JsonPropertyName("tekrar_ariza_sayisi_90g")]
    public double? TekrarArizaSayisi90G { get; set; }

    [Range(0, double.MaxValue), JsonPropertyName("volt_ort_7g")]
    public double? VoltOrt7G { get; set; }

    [Range(0, double.MaxValue), JsonPropertyName("devir_ort_7g")]
    public double? DevirOrt7G { get; set; }

    [Range(0, double.MaxValue), JsonPropertyName("basinc_ort_7g")]
    public double? BasincOrt7G { get; set; }

    [Range(0, double.MaxValue), JsonPropertyName("titresim_ort_7g")]
    public double? TitresimOrt7G { get; set; }

    [Range(0, 100), JsonPropertyName("anomali_puani_7g")]
    public double? AnomaliPuani7G { get; set; }

    [Range(0, 100), JsonPropertyName("uretim_yogunlugu_7g")]
    public double? UretimYogunlugu7G { get; set; }

    [MinLength(1), JsonPropertyName("model")]
    public string? Model { get; set; }

    [AllowedValues("Düşük", "Orta", "Yüksek", "Kritik"), JsonPropertyName("kritik_seviye")]
    public string? KritikSeviye { get; set; }

    [JsonIgnore]
    public bool HasCriticalTelemetry { get; set; }

    [JsonIgnore]
    public bool IsMandatoryMaintenanceOverdue { get; set; }

    [JsonIgnore]
    public bool HasRepeatedCriticalFailure7Days { get; set; }

    [JsonIgnore]
    public decimal DataCompletenessRate
    {
        get
        {
            var values = new object?[]
            {
                Yas, SonBakimdanGecenGun, BakimSayisi30G, BakimSayisi90G,
                ArizaSayisi30G, ArizaSayisi90G, ToplamGecmisAriza,
                HataSayisi7G, HataSayisi30G, DurusSuresi30G,
                OrtMudahaleSuresi90G, TekrarArizaSayisi90G,
                VoltOrt7G, DevirOrt7G, BasincOrt7G, TitresimOrt7G,
                AnomaliPuani7G, UretimYogunlugu7G, Model, KritikSeviye
            };
            var filled = values.Count(value => value is not null &&
                (value is not string text || !string.IsNullOrWhiteSpace(text)));
            return Math.Round(filled * 100m / ModelFeatureCount, 2,
                MidpointRounding.AwayFromZero);
        }
    }
}
