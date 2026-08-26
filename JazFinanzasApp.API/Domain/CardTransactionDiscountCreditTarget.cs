namespace JazFinanzasApp.API.Domain
{
    // Dónde acreditó el banco el reintegro de una promoción (ver plan-reintegro-saldo-tarjeta.md).
    // Account: cayó en una cuenta propia, y el descuento nace materializado al 100%.
    // Card: quedó como saldo a favor de la tarjeta, y se materializa después — cuando el resumen
    // lo absorbe o cuando el usuario lo rescata a una cuenta.
    public static class CardTransactionDiscountCreditTarget
    {
        public const string Account = "ACCOUNT";
        public const string Card = "CARD";

        public static bool IsValid(string? value) => value == Account || value == Card;
    }
}
