DotSpatialProjectionsLite
=========================

A light-weight version of the DotSpatial.Projection library.

This library is a fork of the DotSpatial project at http://dotspatial.codeplex.com/

The following changes have been made to make the library more light weight and reduce
its memory footprint:

* Only include the DotSpatial.Projections portion.
* Move definition of all the coordinate systems to a separate project, so that the main
  project can be used without all the coordinate system definitions.
* Do not include the NAD shift files for using NAD27 coordinate systems.

Forked by Peter Golde.
