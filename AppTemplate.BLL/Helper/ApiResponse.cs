﻿namespace AppTemplate.BLL.Helper
{
    public class ApiResponse<T>
    {
        public int Code { get; set; }
        public string Status { get; set; }
        public T Result { get; set; }
        public string? ErrorId { get; set; }  
    }
}
