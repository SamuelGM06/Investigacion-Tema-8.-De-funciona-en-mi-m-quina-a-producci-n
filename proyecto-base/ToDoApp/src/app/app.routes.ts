import { Routes } from '@angular/router';
import { Home } from './home/home';
import { Login } from './pages/login/login';
import { TodoForm } from './pages/todo-form/todo-form';
import { TodoList } from './pages/todo-list/todo-list';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'login', component: Login },
  { path: 'tasks', component: TodoList },
  { path: 'task/new', component: TodoForm },// crear un componente llamado ListaTareas que se muestre en la ruta /listatareas
  { path: 'task/:id/edit', component: TodoForm }

];


// el comopnente debe tener una lista no ordenada UL de tareas ficticias (lista quemada en el codigo)