
using System;
using TodoApi.Models;
using Xunit;

namespace TodoApi.Tests;

public class TodoItemTests
{
    // Prueba 1: Permitir un cambio de estado valido
    [Fact]
    public void CambiarDePendienteAEnProgreso()
    {
        var tarea = new TodoItem();

        tarea.ChangeStatus(TodoStatus.EnProgreso);

      Assert.Equal(TodoStatus.EnProgreso, tarea.Status);
    }

    // Prueba 2: Rechazar un cambio de estado invalido
    [Fact]
    public void NoPermitirCambiarTareaCompletada()
    {
        var tarea = new TodoItem();

        tarea.ChangeStatus(TodoStatus.EnProgreso);
        tarea.ChangeStatus(TodoStatus.Completada);

        Assert.Throws<InvalidOperationException>(() =>
            tarea.ChangeStatus(TodoStatus.Pendiente));
    }

    // Prueba 3: Registrar la fecha de finalizacion
    [Fact]
    public void RegistrarFechaAlCompletarTarea()
    {
        var tarea = new TodoItem();

        tarea.ChangeStatus(TodoStatus.EnProgreso);
        tarea.ChangeStatus(TodoStatus.Completada);

        Assert.NotNull(tarea.CompletedAt);
    }
}
