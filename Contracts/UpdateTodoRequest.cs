using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoFunction.Contracts;

public class UpdateTodoRequest
{
    public string? Title { get; set; }
    public bool? IsCompleted { get; set; }
}
