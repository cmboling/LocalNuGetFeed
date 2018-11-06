import {filter} from 'rxjs/operators';
import {Component} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {Package} from "../shared/models/package.model";
import {PackageService} from "../services/package.service";

@Component({
  selector: 'package-details',
  templateUrl: './package-details.html'
})
export class PackageDetailsComponent {
  public packageVersions: string[];
  public package: Package;

  constructor(private _packageService: PackageService, private route: ActivatedRoute) {

  }

  ngOnInit() {
    this.route.params.pipe(
      filter(params => params.id))
      .subscribe(params => {
        if (params.id) {
          this.getPackageVersions(params.id);
        }else{
          console.error('Package Id is undefined');
        }
      }, error => {
        console.error(error);
      });

  }

  getPackageVersions(packageId: string) {
    this._packageService.getPackageVersions(packageId).subscribe(
      data => {
        this.packageVersions = data.map(z => z.version);
        this.package = data[0]; // actual package with latest version
      }, error => {
        console.error(error);
      }
    );
  }
}
