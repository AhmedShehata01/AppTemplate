namespace Kindergarten.BLL.Helper
{
    public class ApiResponse<T>
    {
        public int Code { get; set; }
        public string Status { get; set; }
        public T Result { get; set; }
    }
}
