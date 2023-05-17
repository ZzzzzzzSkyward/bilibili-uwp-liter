namespace BiliLite.Models
{
    public class ReturnModel<T>
    {
        public bool success { get; set; }
        public string message { get; set; } = "";

        public T data { get; set; }
    }
}
