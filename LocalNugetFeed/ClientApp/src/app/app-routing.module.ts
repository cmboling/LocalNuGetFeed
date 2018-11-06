import {NgModule} from '@angular/core';
import {Routes, RouterModule} from '@angular/router';
import {HomeComponent} from "./home/home.component";
import {PackageDetailsComponent} from "./package-details/package-details.component";


const routes: Routes = [

  {path: '', redirectTo: 'packages', pathMatch: 'full'},
  {path: 'packages', component: HomeComponent, runGuardsAndResolvers: 'paramsOrQueryParamsChange'},
  {path: 'package/:id', component: PackageDetailsComponent},
  {path: '**', redirectTo: 'packages'}

];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
  providers: [],
})
export class AppRoutingModule {
}
