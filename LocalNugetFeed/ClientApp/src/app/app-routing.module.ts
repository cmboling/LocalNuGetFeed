import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { HomeComponent } from "./home/home.component";
import {APP_BASE_HREF} from "@angular/common";


const routes: Routes = [

  { path: '', redirectTo: '/home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent, runGuardsAndResolvers: 'paramsOrQueryParamsChange' },
  { path: '**', redirectTo: '/home' }

];


@NgModule({
  imports: [RouterModule.forRoot(routes, { enableTracing: true, initialNavigation: 'enabled'  })],
  exports: [RouterModule],
  providers: [{provide: APP_BASE_HREF, useValue: '/'}],
})
export class AppRoutingModule { }
