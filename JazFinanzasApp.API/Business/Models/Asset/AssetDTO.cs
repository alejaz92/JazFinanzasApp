﻿

namespace JazFinanzasApp.API.Business.DTO.Asset
{
    public class AssetDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string AssetTypeName { get; set; }

        public bool IsReference { get; set; }
        public bool IsMainReference { get; set; }
    }
}
