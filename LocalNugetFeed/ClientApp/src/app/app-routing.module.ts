import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import {HomeComponent} from "./home/home.component";


const routes: Routes = [
  { path: '', component: HomeComponent, runGuardsAndResolvers: 'paramsOrQueryParamsChange' },
];


@NgModule({
  imports: [RouterModule.forRoot(routes,{ enableTracing: true } )],
  exports: [RouterModule],
  providers:[],
})
export class AppRoutingModule { }
