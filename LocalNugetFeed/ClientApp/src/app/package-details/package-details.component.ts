import {filter} from 'rxjs/operators';
import {Component} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {Package} from "../shared/models/package.model";
import {PackageService} from "../services/package.service";

@Component({
  selector: 'package-details',
  templateUrl: './package-details.html'
})
export class PackageDetailsComponent {
  public packageVersions: Package[];
  public package: Package;

  constructor(private _packageService: PackageService, private route: ActivatedRoute, private router: Router) {

  }

  ngOnInit() {
    this.route.params.pipe(
      filter(params => params.id))
      .subscribe(params => {
        if (params.id) {
          this.getPackageVersions(params.id);
        } else {
          console.error('Package Id is undefined');
        }
      }, error => {
        console.error(error);
      });

  }

  getPackageInfoByVersion(selectedPackage: Package) {
    this.package = this.packageVersions.filter(x => x.version === selectedPackage.version)[0];
  }

  getPackageVersions(packageId: string) {
    this._packageService.getPackageVersions(packageId).subscribe(
      data => {
        this.packageVersions = data;
        this.getPackageInfoByVersion(data[0]); // actual package with latest version
      }, error => {
        this.router.navigateByUrl('/').then(value => console.error(error));
      }
    );
  }
}
