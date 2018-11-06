import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';
import { AppComponent } from './app.component';
import { HomeComponent } from "./home/home.component";
import { AppRoutingModule } from './app-routing.module';
import {FormsModule} from "@angular/forms";
import {PackageDetailsComponent} from "./package-details/package-details.component";

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    PackageDetailsComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    AppRoutingModule,
    HttpClientModule,
    FormsModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
