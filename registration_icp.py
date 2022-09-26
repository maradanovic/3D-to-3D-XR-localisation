import open3d
import numpy as np
import os
import time

start_time = time.time()

bound_size = 3

np_bound_size = np.full(3, bound_size)

position_estimate = np.loadtxt("data_ar_localization/temp/reloc_position_estimate.txt")

current_relation_matrix = np.loadtxt("data_ar_localization/temp/currentRelationMatrix.txt")

bounding_box = open3d.geometry.AxisAlignedBoundingBox((position_estimate - np_bound_size), (position_estimate + np_bound_size))

reference_cloud = open3d.io.read_point_cloud("data_ar_localization/reference_cloud_bin.ply")

cropped_reference_cloud = reference_cloud.crop(bounding_box)

ar_mesh = open3d.io.read_triangle_mesh("data_ar_localization/reloc_mesh_ascii_0.ply")
verticesNum = len(ar_mesh.vertices)
receivedCloud = ar_mesh.sample_points_uniformly(number_of_points=(verticesNum*20))

def execute_icp(src_cloud, tgt_cloud, distance_threshold, initial_transform):
   result = open3d.registration.registration_icp(
       src_cloud, tgt_cloud, distance_threshold, initial_transform,
       criteria = open3d.registration.ICPConvergenceCriteria(
           relative_fitness = 1.0e-05,
           relative_rmse = 1.0e-05,
           max_iteration = 10))
   return result

result_icp = execute_icp(receivedCloud, cropped_reference_cloud, 0.03, np.identity(4))

result_icp_tm = result_icp.transformation

current_relation_matrix = np.matmul(current_relation_matrix, result_icp_tm)

run_time = time.time() - start_time
print("Result ICP:\n")
print(result_icp)
print("ICP transformation matrix:\n")
print("ICP script runtime:\n")
print(run_time)

if (result_icp.inlier_rmse > 0.03):
    print("ICP registration unsuccessful.")
    exit(1)
else:
    np.savetxt("data_ar_localization/temp/resultTransformationMatrix.txt", result_icp_tm, delimiter=" ")
    np.savetxt("data_ar_localization/temp/currentRelationMatrix.txt", current_relation_matrix, delimiter=" ")
    exit(0)