using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoFunction.Contracts;
public class CreateTodoRequest
{
    public string Title { get; set; } = string.Empty;
}
